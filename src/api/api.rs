use std::{collections::HashMap, sync::Arc, time::Duration};

use leptos::*;
use serde_json::json;
use once_cell::sync::Lazy;

#[cfg(feature = "ssr")]
use tokio::sync::Mutex;
#[cfg(feature = "ssr")]
use super::connect::ApiSession;

#[cfg(feature = "ssr")]
static SESSIONS: Lazy<Mutex<HashMap<u64, ApiSession>>> = Lazy::new(|| {
    Mutex::new(HashMap::new())
});

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

#[server(SendAuth, "/api")]
pub async fn send_auth(session_id: u64) -> Result<Option<()>, ServerFnError> {
    let mut sessions = SESSIONS.lock().await;
    
    if let Some(session) = sessions.get_mut(&session_id) {
        session.send_from_all(json!({
            "method": "register",
            "name": "Pavlo",
            "username": "@fklsdjfkls",
            "password_hash": "asd"
        })).await;
    } else {
        return Ok(None)
    }

    Ok(Some(()))
}

#[server(DropSession, "/api")]
pub async fn drop_session(session_id: u64) -> Result<(), ServerFnError> {
    {
        let mut sessions = SESSIONS.lock().await;
        sessions.remove(&session_id);
    }
    
    Ok(())
}