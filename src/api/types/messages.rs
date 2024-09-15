use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct MessageResponse {
    pub message_id: i32,
    pub is_unread: bool,
    pub from_id: i32,
    pub chat_id: i32, 
    pub date: i64,
    pub reply_to: Option<i32>,
    pub content: Option<String>,
    pub media_datas: Vec<Vec<u8>>,
    pub media_names: Vec<String>
}

#[derive(Debug, Serialize, Deserialize)]
pub struct FetchMessagesResponse {
    ok: bool,
    method: String,
    pub data: Option<Vec<MessageResponse>>
}

