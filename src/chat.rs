use std::time::{SystemTime, UNIX_EPOCH};

use chrono::{DateTime, Local, TimeZone, Timelike};
use leptos::*;
use wasm_bindgen::JsCast;
use web_sys::HtmlInputElement;

use crate::{api::{api::send_message, types::{chat::ChatInfo, messages::MessageResponse}}, bar::use_self_context, theme::use_theme, types::Theme};

fn messages_placeholder(right_corner: bool) -> impl IntoView {
    let mut base = "flex items-start space-x-4 w-full".to_string();
    if right_corner {
        base.push_str(" justify-end");
    }
    view !{
        <div class={base}>
        {if !right_corner {view!{<div class="animate-pulse rounded-full bg-gray-300 dark:bg-slate-700 h-10 w-10 outline outline-gray-500 dark:outline-slate-500"></div>}} else {view!{<div></div>}}}
            <div class="animate-pulse flex-1 space-y-6 py-1 max-w-lg">
                <div class="space-y-3">
                    <div class="animate-pulse grid grid-cols-3 gap-4">
                        <div class="animate-pulse h-4 bg-gray-300 dark:bg-slate-700 rounded col-span-2 outline outline-gray-500 dark:outline-slate-500"></div>
                        <div class="animate-pulse h-4 bg-gray-300 dark:bg-slate-700 rounded col-span-1 outline outline-gray-500 dark:outline-slate-500"></div>
                    </div>
                    <div class="animate-pulse h-4 bg-gray-300 dark:bg-slate-700 rounded outline outline-gray-500 dark:outline-slate-500"></div>
                </div>
            </div>
        {if right_corner {view!{<div class="animate-pulse rounded-full bg-gray-300 dark:bg-slate-700 h-10 w-10 outline outline-gray-500 dark:outline-slate-500"></div>}} else {view!{<div></div>}}}
        
        </div>
    }
}

enum Status {
    Pending,
    Sent,
    Read
}

fn message_view(from_me: bool, status: Status, text: &String, date: i64) -> impl IntoView {
    let local = Local.timestamp_opt(date, 0).unwrap();
    let time = local.format("%H:%M").to_string();
    let alignment_class = if from_me { "ml-auto mr-2" } else { "ml-2 mr-auto" };

    let bg_class = if from_me {
        "bg-gradient-to-r from-sky-500/10 to-sky-700/10 bg-no-repeat bg-[length:200%_200%] dark:text-white"
    } else {
        "bg-gray-100/50 dark:bg-slate-800/25".into()
    };

    view! {
        <div 
            style="margin-top: 5px; margin-bottom: 5px; min-width: 75px; max-width: 700px; min-height: 3rem; max-height: 100em;"
            class=format!("z-10 flex transition-theme rounded-2xl shadow-lg {} {}", bg_class, alignment_class)>
            <p class="text-black truncate dark:text-gray-300 text-sm pl-2 my-3">
                {text}
            </p>
            <div class="relative flex flex-row mb-0 mt-auto" style="bottom: 2px; right: 6px;">
                <p class="text-xs text-gray-800 dark:text-gray-400">{time}</p>
                {if from_me {view!{
                    <div class="ml-1">
                    {match status {
                        Status::Read => view! {
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                                <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                            </svg>
                        },
                        Status::Sent => view! {
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 0 0 1.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.451 10.451 0 0 1 12 4.5c4.756 0 8.773 3.162 10.065 7.498a10.522 10.522 0 0 1-4.293 5.774M6.228 6.228 3 3m3.228 3.228 3.65 3.65m7.894 7.894L21 21m-3.228-3.228-3.65-3.65m0 0a3 3 0 1 0-4.243-4.243m4.242 4.242L9.88 9.88" />
                            </svg>
                        },
                        Status::Pending => view! {
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 animate-pulse">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                            </svg>
                        }
                    }}
                  </div>
                }} else {view! {<div></div>}}}
            </div>
        </div>
    }
}

pub fn use_current_chat() -> RwSignal<Option<ChatInfo>> {
    use_context().expect("current chat context to be defined")
}

pub fn provide_current_chat() {
    let rw_current_chat: RwSignal<Option<ChatInfo>> = create_rw_signal(None);
    provide_context(rw_current_chat);
}

#[component]
pub fn Chat() -> impl IntoView {
    let session_uuid = use_context::<Resource<(), u64>>().unwrap();

    let rw_current_chat = use_current_chat();
    let rw_self_user = use_self_context();
    let (message, set_message) = create_signal("".to_string());

    let on_send = move |e: leptos::ev::SubmitEvent| {
        e.prevent_default();
        let message = message.get();
        let session_id = session_uuid.get();
        let chat = rw_current_chat.get();

        if !message.is_empty() {
            if let Some(session_id) = session_id {
                if let Some(mut chat) = chat {
                    spawn_local(async move {
                        let now = chrono::Utc::now().timestamp() as i64;

                        chat.messages.push(MessageResponse {
                            message_id: chat.messages.last().unwrap().message_id + 1,
                            is_unread: None,
                            from_id: rw_self_user.get_untracked().unwrap().user_id,
                            chat_id: 0,
                            date: now,
                            reply_to: None,
                            content: Some(message.clone()),
                            media_hashes: vec![],
                            media_names: vec![]
                        });
                        rw_current_chat.set(Some(chat.clone()));
                        set_message.set("".into());
                        let sent_res = send_message(session_id, chat.chat_id, message).await.unwrap();

                        if sent_res {
                            chat.messages.iter_mut().last().unwrap().is_unread = Some(true);
                            rw_current_chat.set(Some(chat));
                        }

                        let document = window().document().unwrap();
                        if let Some(input) = document.get_element_by_id("message") {
                            let input: HtmlInputElement = input.dyn_into().unwrap();
                            input.focus().unwrap();
                        }
                    });
                }
            }
        }
    };

    view! {
        <Suspense fallback=move || view! { 
            <div class="flex flex-col h-screen w-screen p-4 space-y-4 overflow-auto">
                {
                    let mut to_view: Vec<_> = vec![];
                    for _ in 0..12 {
                        let right_corner = rand::random::<bool>();
                        to_view.push(view!{<div class="flex-grow">{messages_placeholder(right_corner)}</div>});
                    }

                    to_view
                }
            </div>
        }>
            // TODO: remove this fcking placeholder
            {session_uuid.get();}
            <div class="flex w-full flex-col h-full">
                <div class="relative z-10 w-full left-300 top-0">
                    <div class="w-full max-h-[50px] flex flex-row items-center px-2">
                        <div class="flex flex-col p-1">
                            {
                                move || {
                                    let current_chat = rw_current_chat.get();

                                    match current_chat {
                                        Some(chat) => view! {
                                            <div>
                                                <h1 class="font-extrabold text-md">{chat.name}</h1>
                                                <h1 class="text-sm text-gray-400 dark:text-gray-500">Last seen recently</h1>
                                            </div>
                                        },
                                        None => view! {
                                            <div></div>
                                        }
                                    }
                                }
                            }
                        </div>
                    </div>
                    {move || {
                        let current_chat = rw_current_chat.get();

                        match current_chat {
                            Some(_) => view!{<div><hr class="border-gray-600 opacity-25 dark:border-gray-600"/></div>},
                            None => view!{<div></div>}
                        }
                    }}
                </div>

                <div class="h-full overflow-auto flex flex-col-reverse">
                    {   
                        move || {
                            let current_chat = rw_current_chat.get();
                            match current_chat {
                                Some(chat) => view! {
                                    <div class="h-full overflow-auto flex flex-col-reverse">
                                        {
                                            let self_info = rw_self_user.get();

                                            if let Some(self_info) = self_info {
                                                chat.messages.iter().map(|message| {
                                                    let from_me = if message.from_id == self_info.user_id {true} else {false};
                                                    message_view(from_me, match message.is_unread {
                                                        Some(message) => if !message {Status::Read} else {Status::Sent},
                                                        None => Status::Pending
                                                    }, message.content.as_ref().unwrap(), message.date)
                                                }).rev().collect::<Vec<_>>()
                                            } else {
                                                todo!()
                                            }
                                        }
                                    </div>
                                },
                                None => view! {
                                    <div class="flex flex-col justify-center items-center mt-auto mb-auto">
                                        <img class="w-24 h-24 mb-5 ml-auto mr-auto" src=move || {
                                            let theme = use_theme().get();
                    
                                            match theme {
                                                Some(theme) => {
                                                    match theme {
                                                        Theme::Light => {"logo_black.png"},
                                                        Theme::Dark => {"logo.png"}
                                                    }
                                                }
                                                None => {"logo_black.png"}
                                            }
                                        }/>
                                        <h1 class="font-extrabold text-2xl">Welcome to PPgram!</h1>
                                        <p>Start by selecting or creating a chat</p>
                                    </div>
                                }
                            }
                        }
                    }
                </div>

                <div class="relative w-fullleft-300 bottom-0">
                    {move || {
                        let current_chat = rw_current_chat.get();

                        match current_chat {
                            Some(_) => view!{
                                <div>
                                <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                                </div>
                            },
                            None => view!{<div></div>}
                        }
                    }}
                    <div class="flex flex-row  min-h-[75px] items-center px-2">
                        <div class="flex flex-col w-full my-4">
                            {move || {
                                let current_chat = rw_current_chat.get();

                                match current_chat {
                                    Some(_) => view!{
                                        <div class="transition-theme flex items-center 
                                            rounded-xl 
                                            bg-gray-100/70 dark:bg-slate-700/50 dark:border-slate-600 
                                            focus-within:ring-2 focus-within:ring-sky-500 dark:focus-within:ring-sky-500">
                                        <div class="ml-3 mr-3">
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="m18.375 12.739-7.693 7.693a4.5 4.5 0 0 1-6.364-6.364l10.94-10.94A3 3 0 1 1 19.5 7.372L8.552 18.32m.009-.01-.01.01m5.699-9.941-7.81 7.81a1.5 1.5 0 0 0 2.112 2.13" />
                                        </svg>
                                        </div>
                                        <form class="flex flex-row w-full" on:submit=on_send autocomplete="off">
                                            <input
                                                on:input=move |ev| {
                                                    set_message.set(event_target_value(&ev));
                                                }
                                                prop:value=message
                                                type="text"
                                                id="message"
                                                name="message"
                                                required
                                                class="transition-theme py-3 w-full bg-gray-100/0 dark:bg-slate-700/0 bg-opacity-100 text-black dark:text-white focus:outline-none"
                                                placeholder="Your Message"
                                                />
                                            <button 
                                            class="ml-2 mr-2">
                                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5" />
                                            </svg>
                                            </button>
                                        </form>
                                        </div>
                                    },
                                    None => view!{<div></div>}
                                }
                            }}
                        </div>
                    </div>
                </div>
            </div>
        </Suspense>
    }
}