use std::{collections::HashMap, sync::Arc, time::Duration};

use leptos::*;
use once_cell::sync::Lazy;
use serde::Deserialize;
use serde_json::{json, Value};
use tracing::info;

use crate::types::AuthCredentials;

#[cfg(feature = "ssr")]
use super::connect::ApiSession;
use super::types::auth::{ErrorResponse, UserInfo, LoginResponse, RegisterResponse};
#[cfg(feature = "ssr")]
use tokio::sync::Mutex;

#[cfg(feature = "ssr")]
static SESSIONS: Lazy<Mutex<HashMap<u64, ApiSession>>> = Lazy::new(|| Mutex::new(HashMap::new()));

#[server(CreateSession, "/api")]
pub async fn create_session() -> Result<u64, ServerFnError> {
    tokio::time::sleep(std::time::Duration::from_secs(1)).await;
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
    creds: AuthCredentials
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
