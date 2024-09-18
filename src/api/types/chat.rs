use serde::{Deserialize, Serialize};

use super::{auth::UserInfo, messages::MessageResponse};

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
    pub chats: Vec<FetchChatsDetails>
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct LastMessageInfo {
    pub sender: UserInfo,
    pub message: MessageResponse
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct ChatView {
    pub name: String,
    pub chat_id: i32,
    pub username: String,
    pub photo: Option<String>,
    pub last_message: Option<LastMessageInfo>
}

#[derive(Debug, Clone, Deserialize, Serialize)]
pub struct ChatInfo {
    pub name: String,
    pub chat_id: i32,
    // pub participants: Vec<i32>
    pub photo: Option<String>,
    pub username: String,
    pub messages: Box<Vec<MessageResponse>>
}