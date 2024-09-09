use leptos::logging;

use super::builder::MessageBuilder;

pub struct MessageHandler {
    builder: Option<MessageBuilder>,
    is_first: bool
}

impl MessageHandler {
    pub fn new() -> Self {
        Self {
            builder: None,
            is_first: true
        }
    }

    pub fn try_handle_json(&mut self) {
        let builder = self.builder.as_mut().unwrap();
        let content = builder.content_utf8().unwrap();

        logging::log!("{}", content);
    }

    pub fn handle_segmented_frame(&mut self, buffer: &[u8]) {
        if self.is_first {
            self.builder = MessageBuilder::parse(buffer);
            if let Some(builder) = &self.builder {
                logging::log!("Got the message! \n Message size: {}", builder.size());
            }
            self.is_first = false;

            if let Some(ref message) = self.builder {
                if !message.ready() {
                    return;
                }
            }
        }
        
        let mut do_handle = false;
        if let Some(ref mut message) = self.builder {
            if !message.ready() {
                message.extend(buffer);
            }
            
            if message.ready() {
                do_handle = true;
            }
        }

        if do_handle {
            if let Some(_) = &self.builder {
                self.try_handle_json();
            }
    
            if let Some(ref mut message) = self.builder {
                message.clear();
            }
            self.builder = None;
            self.is_first = true;
        }
    }
}