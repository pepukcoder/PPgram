use std::net::{TcpListener, TcpStream};

use crate::{api::{self, api::create_session}, auth::{use_auth_creds, Auth}, bar::Bar, chat::Chat, error_template::{AppError, ErrorTemplate}, theme::ThemeToggler, types::AuthCredentials};
use leptos::*;
use leptos_meta::*;
use leptos_router::*;
use leptos_use::on_click_outside;
use serde_json::json;
use wasm_bindgen::{prelude::{wasm_bindgen, Closure}, JsCast};
use super::theme::ThemeProvider;

#[component]
pub fn App() -> impl IntoView {
    // Provides context that manages stylesheets, titles, meta tags, etc.
    provide_meta_context();

    {
        use leptos_use::use_favicon;
        let (_, set_favicon) = use_favicon();
        set_favicon.set(Some("favicon.ico".into()));
    }

    view! {
        <Stylesheet id="leptos" href="/pkg/ppgram-web.css"/>

        <Title text="PPgram Web"/>

        <link rel="preconnect" href="https://fonts.googleapis.com"/>
        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin/>
        <link href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap" rel="stylesheet"/>
        <Router fallback=|| {
            let mut outside_errors = Errors::default();
            outside_errors.insert_with_default_key(AppError::NotFound);
            view! {
                <ErrorTemplate outside_errors/>
            }
            .into_view()
        }>
            <main>
                <ThemeProvider>
                    <Routes>
                        <Route path="" view=HomePage/>
                        <Route path="/auth" view=Auth/>
                    </Routes>
                </ThemeProvider>
            </main>
        </Router>
    }
}

/// Renders the home page of your application.
#[component]
fn HomePage() -> impl IntoView {
    // Connect to the server and provide children session_id
    let session_id = create_local_resource(|| (), |_| async move { create_session().await.unwrap() });
    provide_context(session_id);

    // Check if cookies exist and provide the state to the children
    {
        use leptos_use::*;
        use codee::string::JsonSerdeCodec;
        let auth: (Signal<Option<AuthCredentials>>, WriteSignal<Option<AuthCredentials>>) = use_cookie::<AuthCredentials, JsonSerdeCodec>("auth_credentials");
        provide_context(auth);
    }
    
    // TODO: Remove this shitcode
    // Closing connection when user leaves(or reloads) website
    create_effect(move |_| {
        let callback = Closure::wrap(Box::new(move || {
            use crate::api::api::drop_session;
            let uuid = session_id.get().unwrap();
            spawn_local(async move {
                drop_session(uuid).await.unwrap();
            });
        }) as Box<dyn FnMut()>);
    
        // TODO: Remove this shitcode

        // Works only when user leaves website
        window().add_event_listener_with_callback("beforeunload", callback.as_ref().unchecked_ref()).ok();
        // Works only when user reloads
        window().add_event_listener_with_callback("unload", callback.as_ref().unchecked_ref()).ok();
        // Combining methods to get needed result

        callback.forget();
    });

    let (maybe_auth_creds, _) = use_auth_creds();

    if let Some(creds) = maybe_auth_creds.get() {
        view! {
            <div class="flex transition-theme h-screen text-black bg-white dark:text-white dark:bg-slate-900">
                <div class="absolute inset-0 z-0">
                    <div class="blob-gradient"></div>
                </div>
                <div class="flex flex-row w-full z-10">
                    {Bar()}
                    <div style="width: 0.75px;" class="bg-gray-600 dark:bg-gray-600 opacity-25"></div>
                    {Chat()}
                </div>
            </div>
        }
    }
    else {
        view! {
            <div>
                {Auth()}
            </div>
        }
    }
}
