use std::str::FromStr;

use serde::{Deserialize, Serialize};

#[derive(Clone, PartialEq, PartialOrd)]
pub enum Theme {
    Dark,
    Light,
}

impl FromStr for Theme {
    type Err = ();

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match s.to_ascii_lowercase().as_str() {
            "light" => Ok(Theme::Light),
            "dark" => Ok(Theme::Dark),
            _ => Err(())
        }
    }
}

impl Default for Theme {
    fn default() -> Self {
        Theme::Dark
    }
}

impl std::fmt::Display for Theme {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.to_string())
    }
}

impl Theme {
    /// Converts the `Theme` variant into a corresponding string.
    pub fn to_string(&self) -> String {
        match self {
            Theme::Light => "light".to_string(),
            Theme::Dark => "dark".to_string(),
        }
    }
}

impl leptos::IntoView for Theme {
    fn into_view(self) -> leptos::View {
        self.to_string().into_view()
    }
}

#[derive(Clone, Serialize, Deserialize)]
pub struct AuthCredentials {
    pub user_id: i32,
    pub session_id: String
}