pub mod app;
pub mod error_template;
pub mod theme;
pub mod types;
pub mod bar;
pub mod api;
pub mod chat;
pub mod auth;
#[cfg(feature = "ssr")]
pub mod fileserv;

#[cfg(feature = "hydrate")]
#[wasm_bindgen::prelude::wasm_bindgen]
pub fn hydrate() {
    use crate::app::*;
    console_error_panic_hook::set_once();
    leptos::mount_to_body(App);
}
