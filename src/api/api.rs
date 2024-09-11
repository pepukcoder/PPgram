use std::{collections::HashMap, sync::Arc, time::Duration};

use leptos::*;
use once_cell::sync::Lazy;
use serde::Deserialize;
use serde_json::{json, Value};

#[cfg(feature = "ssr")]
use super::connect::ApiSession;
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

#[server(SendRegister, "/api")]
pub async fn send_register(
    session_id: u64,
    name: String,
    username: String,
    password: String,
) -> Result<Value, ServerFnError> {
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

        return Ok(res);
    } else {
        return Err(ServerFnError::ServerError(
            "Failed to get session by the passed session_id!".to_string(),
        ));
    }
}
