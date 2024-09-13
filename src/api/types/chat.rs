use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Deserialize, Serialize)]
pub(crate) struct FetchChatsDetails {
    pub name: String,
    pub chat_id: i32, // User/Group ChatId
    pub photo: Option<String>,
    pub username: String,
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub(crate) struct FetchChatsResponse {
    ok: bool,
    method: String,
    pub data: Option<Vec<FetchChatsDetails>>
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct LastMessageData {
    pub name: String,
    pub message: String
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct ChatView {
    pub name: String,
    pub chat_id: i32,
    pub username: String,
    pub photo: Option<String>,
    pub last_message: Option<LastMessageData>
}