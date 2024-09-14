use std::{collections::HashMap, sync::Arc, time::Duration};

use leptos::*;
use once_cell::sync::Lazy;
use serde::Deserialize;
use serde_json::{from_value, json, Value};
use tracing::info;


use crate::api::types::chat::LastMessageData;

#[cfg(feature = "ssr")]
use super::connect::ApiSession;
use super::types::{auth::{ErrorResponse, LoginResponse, RegisterResponse, UserInfo}, chat::{ChatView, FetchChatsResponse}};
#[cfg(feature = "ssr")]
use tokio::sync::Mutex;

#[cfg(feature = "ssr")]
static SESSIONS: Lazy<Mutex<HashMap<u64, ApiSession>>> = Lazy::new(|| Mutex::new(HashMap::new()));

#[server(CreateSession, "/api")]
pub async fn create_session() -> Result<u64, ServerFnError> {
    let session = ApiSession::new().await;

    {
        session.read_from_all();
    }

    let id = rand::random::<u64>();
    {
        let mut sessions = SESSIONS.lock().await;
        sessions.insert(id, session);
    }
    Ok(id)
}

#[server(DropSession, "/api")]
pub async fn drop_session(session_id: u64) -> Result<(), ServerFnError> {
    {
        let mut sessions = SESSIONS.lock().await;
        sessions.remove(&session_id);
    }

    Ok(())
}

#[server(ReopenSession, "/api")]
pub async fn reopen_session(session_id: u64) -> Result<(), ServerFnError> {
    {
        let mut sessions = SESSIONS.lock().await;
        sessions.remove(&session_id);
        let session = ApiSession::new().await;
        sessions.insert(session_id, session);
    }

    Ok(())
}


#[server(SendRegister, "/api")]
pub async fn send_register(
    session_id: u64,
    name: String,
    username: String,
    password: String,
) -> Result<Result<RegisterResponse, ErrorResponse>, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;

    if let Some(session) = sessions.get_mut(&session_id) {
        let res = session
            .send_with_response(json!({
                "method": "register",
                "name": name,
                "username": username,
                "password_hash": password
            }))
            .await
            .unwrap();

        if let Ok(val) = serde_json::from_value::<RegisterResponse>(res.clone()) {
            return Ok(Ok(val));
        } else if let Some(err) = serde_json::from_value(res).unwrap() {
            return Ok(Err(err));
        } else {
            return Err(ServerFnError::ServerError("Failed to parse response!".into()))
        }
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}

#[server(SendLogin, "/api")]
pub async fn send_login(
    session_id: u64,
    username: String,
    password: String,
) -> Result<Result<LoginResponse, ErrorResponse>, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;

    if let Some(session) = sessions.get_mut(&session_id) {
        let res = session
            .send_with_response(json!({
                "method": "login",
                "username": username,
                "password_hash": password
            }))
            .await
            .unwrap();

        if let Ok(val) = serde_json::from_value::<LoginResponse>(res.clone()) {
            return Ok(Ok(val));
        } else if let Some(err) = serde_json::from_value(res).unwrap() {
            return Ok(Err(err));
        } else {
            return Err(ServerFnError::ServerError("Failed to parse response!".into()))
        }
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}

#[server(FetchSelf, "/api")]
pub async fn fetch_self(
    session_id: u64,
) -> Result<Result<UserInfo, ErrorResponse>, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;

    if let Some(session) = sessions.get_mut(&session_id) {
        let res = session
            .send_with_response(json!({
                "method": "fetch",
                "what": "self",
            }))
            .await
            .unwrap();

        if let Ok(val) = serde_json::from_value(res.clone()) {
            return Ok(Ok(val));
        } else if let Some(err) = serde_json::from_value(res).unwrap() {
            return Ok(Err(err));
        } else {
            return Err(ServerFnError::ServerError("Failed to parse response!".into()))
        }
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}


#[server(SendAuth, "/api")]
pub async fn send_auth(
    session_id: u64,
    creds: crate::types::AuthCredentials
) -> Result<bool, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;

    if let Some(session) = sessions.get_mut(&session_id) {
        let res = session
            .send_with_response(json!({
                "method": "auth",
                "session_id": creds.session_id,
                "user_id": creds.user_id
            }))
            .await
            .unwrap();

        if let Some(val) = res.get("ok").map(|v| v.as_bool()) {
            return Ok(val.unwrap() == true);
        }
        else {
            return Err(ServerFnError::ServerError("Failed to parse response!".into()))
        }
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}

#[server(FetchChats, "/api")]
pub async fn fetch_chats(
    session_id: u64
) -> Result<Vec<ChatView>, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;

    if let Some(session) = sessions.get_mut(&session_id) {
        let res = session
            .send_with_response(json!({
                "method": "fetch",
                "what": "chats"
            }))
            .await
            .unwrap();

        let response: FetchChatsResponse = serde_json::from_value(res).unwrap();

        let mut out: Vec<ChatView> = vec![];
        match response.data {
            Some(chats) => {
                for chat in chats {
                    let res = session
                        .send_with_response(json!({
                            "method": "fetch",
                            "what": "messages",
                            "chat_id": chat.chat_id,
                            "range": [-1, 0]
                        }))
                        .await
                        .unwrap();
                    let response: crate::api::types::messages::FetchMessagesResponse = serde_json::from_value(res).unwrap();

                    let view = ChatView {
                        name: chat.name,
                        chat_id: chat.chat_id,
                        username: chat.username,
                        photo: chat.photo,
                        last_message: if let Some(messages) = response.data {
                            if let Some(content) = messages.get(0) {
                                let res = session
                                    .send_with_response(json!({
                                        "method": "fetch",
                                        "what": "user",
                                        "user_id": content.from_id
                                    }))
                                    .await
                                    .unwrap();
                                let user_info: UserInfo = serde_json::from_value(res).unwrap();
                                
                                // Handle media messages by leaving empty
                                Some(LastMessageData {name: user_info.data.name, message: content.content.clone().unwrap_or("".to_string())})
                            } else {
                                None
                            }
                        } else {None}
                    };
                    out.push(view);
                }
            }
            None => {}
        }

        return Ok(out);
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}