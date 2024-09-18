use serde::{Deserialize, Serialize};

#[derive(Deserialize, Serialize, Clone, Default, Debug)]
pub struct ErrorResponse {
    pub(crate) ok: bool,
    pub(crate) method: String,
    pub error: String
}

impl std::fmt::Display for ErrorResponse {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "Method: {}! Error: {}", self.method, self.error)
    }
}

#[derive(Deserialize, Serialize, Clone)]
pub struct RegisterResponse {
    ok: bool,
    pub user_id: i32,
    pub session_id: String
}

pub type LoginResponse = RegisterResponse;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserInfo {
    ok: bool,
    method: String,
    pub name: String,
    pub user_id: i32,
    pub username: String,
    pub photo: Option<String>,
}