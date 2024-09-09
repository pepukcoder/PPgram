use std::{sync::Arc, time::{Duration, Instant}};

use serde::Serialize;
#[cfg(feature = "ssr")]
use tokio::{time, io::{AsyncReadExt, AsyncWriteExt}, net::tcp::{OwnedReadHalf, OwnedWriteHalf}, sync::Mutex};

use super::{builder::MessageBuilder, handler::MessageHandler};

static TIMEOUT_DURATION: Duration = Duration::new(1000, 0);

#[cfg(feature = "ssr")]
struct ApiConnection {
    reader: Arc<Mutex<OwnedReadHalf>>,
    writer: Arc<Mutex<OwnedWriteHalf>>
}

#[cfg(feature = "ssr")]
impl ApiConnection {
    pub async fn new() -> Self {
        use tokio::net::TcpStream;

        let stream = TcpStream::connect("0.0.0.0:8080").await.expect("Failed to create connection");
        let (reader, writer) = stream.into_split();

        Self {
            reader: Arc::new(Mutex::new(reader)),
            writer: Arc::new(Mutex::new(writer))
        }
    }

    pub async fn send_message_json<T: Serialize + Send + 'static>(&self, msg: T) {
        let writer = Arc::clone(&self.writer);
        let mut writer_guard = writer.lock().await;
        writer_guard.write(&MessageBuilder::build_from_str(serde_json::to_string(&msg).unwrap()).packed()).await.unwrap();
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
                            handler.handle_segmented_frame(&buffer[0..n]);
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
    connections: Vec<ApiConnection>,
    last_usage: Instant
}
#[cfg(feature = "ssr")]
impl ApiSession {
    pub async fn new() -> Self {
        Self {
            connections: vec![ApiConnection::new().await],
            last_usage: Instant::now(),
        }
    }

    async fn send_from(&mut self, idx: usize, msg: impl Serialize + Clone + Send + 'static) {
        self.last_usage = Instant::now();
        self.connections[idx].send_message_json(msg).await;
    }

    pub async fn send_from_all(&mut self, msg: impl Serialize + Clone + Send + 'static) {
        for i in 0..self.connections.len() {
            self.send_from(i, msg.clone()).await;
        } 
    }

    pub fn read_from_all(&self) {
        for con in self.connections.iter() {
            con.launch_receiver_loop();
        } 
    }
}
