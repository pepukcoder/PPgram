use serde::{Deserialize, Serialize};

#[derive(Deserialize, Serialize, Clone)]
pub struct ErrorResponse {
    ok: bool,
    method: String,
    error: String
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

#[derive(Deserialize, Serialize, Clone, Default)]
pub struct FetchDataResponse {
    pub name: String,
    pub user_id: i32,
    pub username: String,
    pub photo: Option<String>
}

#[derive(Deserialize, Serialize, Clone, Default)]
pub struct UserInfo {
    ok: bool,
    method: String,
    pub data: FetchDataResponse
}