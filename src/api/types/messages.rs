use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct MessageResponse {
    pub message_id: i32,
    pub is_unread: Option<bool>,
    pub from_id: i32,
    pub chat_id: i32, 
    pub date: i64,
    pub reply_to: Option<i32>,
    pub content: Option<String>,
    pub media_hashes: Vec<String>,
    pub media_names: Vec<String>
}

#[derive(Serialize, Deserialize)]
pub struct FetchMessagesResponse {
    ok: bool,
    method: String,
    pub messages: Vec<MessageResponse>
}