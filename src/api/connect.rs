use std::{
    sync::Arc,
    time::{Duration, Instant},
};

use leptos::logging;
use serde::{Deserialize, Serialize};
#[cfg(feature = "ssr")]
use tokio::{
    io::{AsyncReadExt, AsyncWriteExt},
    net::tcp::{OwnedReadHalf, OwnedWriteHalf},
    sync::{Mutex, mpsc},
    time,
};

use super::{builder::MessageBuilder, handler::MessageHandler, types::auth::ErrorResponse};

#[cfg(feature = "ssr")]
struct ApiConnection {
    reader: Arc<Mutex<OwnedReadHalf>>,
    writer: Arc<Mutex<OwnedWriteHalf>>,
}

#[cfg(feature = "ssr")]
impl ApiConnection {
    pub async fn new() -> Self {
        use tokio::net::TcpStream;

        let stream = TcpStream::connect("0.0.0.0:8080")
            .await
            .expect("Failed to create connection");
        let (reader, writer) = stream.into_split();

        Self {
            reader: Arc::new(Mutex::new(reader)),
            writer: Arc::new(Mutex::new(writer)),
        }
    }

    pub async fn send_message_json<T: Serialize + Send + 'static>(&self, msg: T) {
        let writer = Arc::clone(&self.writer);
        let mut writer_guard = writer.lock().await;
        writer_guard
            .write(&MessageBuilder::build_from_str(serde_json::to_string(&msg).unwrap()).packed())
            .await
            .unwrap();
    }

    /// Sends a message and waits for the response.
    /// 
    /// It's not guaranteed that the response is on the message you sent 
    pub async fn send_message_with_response<T: Serialize + Send + 'static>(&self, msg: T) -> Option<serde_json::Value> {
        self.send_message_json(msg).await;
        
        let mut handler_once = MessageHandler::new();
        let mut reader_guard = self.reader.lock().await;
        let mut result: Option<serde_json::Value> = None;
    
        loop {
            let mut buffer = [0; 1024];
    
            match reader_guard.read(&mut buffer).await {
                Ok(0) => break, // Connection closed, exit the loop
                Ok(n) => {
                    handler_once.handle_segmented_frame(&buffer[0..n], |builder| {
                        let content = builder.content_utf8().unwrap();
                        // Store the result in the `result` variable
                        result = Some(serde_json::from_str(content).unwrap());
                    });

                    if result.is_some() {break;}
                }
                Err(_) => break, // An error occurred, exit the loop
            }
        }
    
        result // Return the stored result
    }

    pub fn launch_receiver_loop(&self) {
        tokio::spawn({
            let reader = Arc::clone(&self.reader);
            async move {
                let mut reader_guard = reader.lock().await;
                let mut handler = MessageHandler::new();

                loop {
                    let mut buffer = [0; 1024];

                    match reader_guard.read(&mut buffer).await {
                        Ok(0) => break,
                        Ok(n) => {
                            handler.handle_segmented_frame(&buffer[0..n], 
                    |builder| {
                                    let content = builder.content_utf8().unwrap();

                                    logging::log!("{}", content);
                                }
                            );
                        }
                        Err(_) => break,
                    }
                }
            }
        });
    }
}

#[cfg(feature = "ssr")]
pub struct ApiSession {
    connection: ApiConnection,
    last_usage: Instant
}
#[cfg(feature = "ssr")]
impl ApiSession {
    pub async fn new() -> Self {
        Self {
            connection: ApiConnection::new().await,
            last_usage: Instant::now()
        }
    }

    pub async fn send(&mut self, msg: impl Serialize + Clone + Send + 'static) {
        self.last_usage = Instant::now();
        self.connection.send_message_json(msg).await;
    }

    pub async fn send_with_response<T: for<'a> Deserialize<'a>>(&mut self, msg: impl Serialize + Clone + Send + 'static) -> Result<T, ErrorResponse> {
        self.last_usage = Instant::now();
        let value = self.connection.send_message_with_response(msg).await.unwrap();

        match serde_json::from_value::<T>(value.clone()) {
            Ok(res) => return Ok(res),
            Err(_) => match serde_json::from_value::<ErrorResponse>(value) {
                Ok(res) => {return Err(res)},
                Err(_) => {unimplemented!()}
            }
        }
    }

    pub fn read_from_all(&self) {
        // self.connection.launch_receiver_loop();
    }
}
