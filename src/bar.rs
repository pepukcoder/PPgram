use std::{borrow::Cow, default};

use leptos::*;
use rand::Rng;

use crate::{api::{api::{drop_session, fetch_chats, fetch_self, reopen_session}, types::{auth::UserInfo, chat::ChatView}}, auth::{use_cookies_auth, use_is_authenticated}, theme::ThemeToggler, types::AuthCredentials};

fn chat_placeholder() -> impl IntoView {
    let mut rng = rand::thread_rng();
    let name_percent = rng.gen_range(30..80);
    let message_percent = rng.gen_range(40..100);

    view! {
        <div class="flex p-2 items-start space-x-4 w-full">
            <div class="animate-pulse rounded-full bg-gray-300 dark:bg-slate-700 h-10 w-10 outline outline-2 outline-gray-500 dark:outline-slate-500"></div>
            <div class="flex flex-col w-[80%]">
                <div class="animate-pulse h-4 bg-gray-300 dark:bg-slate-700 rounded outline outline-2 outline-gray-500 dark:outline-slate-500" style={format!("width: {}%", name_percent)}></div>
                <div class="animate-pulse mt-2 h-4 bg-gray-300 dark:bg-slate-700 rounded outline outline-2 outline-gray-500 dark:outline-slate-500" style={format!("width: {}%", message_percent)}></div>
            </div>
        </div>
    }
}

fn chat_view(title: &String, last_message: &String, last_message_sender: &String) -> impl IntoView {
    view! {
        <div class="flex flex-col rounded-lg">
            <button on:click=move |_| {
                logging::log!("Huy")
            }
            class="fade-in transition-all duration-300 ease-in-out flex items-center p-2 space-x-3 hover:bg-gray-100 dark:hover:bg-slate-800 justify-start">
                <img class="w-12 h-12" src="default_avatar.png"/>
                <div>
                    <h1 class="text-xl text-left font-extrabold">{title}</h1>
                    <p class="text-sm flex justify-start text-gray-600 dark:text-gray-400 truncate w-56"><span class="text-sm font-bold text-gray-500 dark:text-gray-200 mr-1">{last_message_sender}</span> {last_message}</p>
                </div>
            </button>
        </div>
        <hr class="border-gray-600 z-0 opacity-25 dark:border-gray-600"/>
    }
}

#[component]
#[allow(unused_braces)]
pub fn Bar() -> impl IntoView {
    let session_uuid = use_context::<Resource<(), u64>>().unwrap();

    let (show_menu, set_show_menu) = create_signal(false);

    let (fetched_user, set_fetched_user) = create_signal(Option::<UserInfo>::None);

    let (fetched_chats, set_fetched_chats) = create_signal(Option::<Vec<ChatView>>::None);

    let (_, set_cookies_auth) = use_cookies_auth();
    let rw_is_authenticated = use_is_authenticated();
    
    create_effect(move |_| {
        let is_auth = rw_is_authenticated.get();
        let fetched_user = fetched_user.get();
        
        if fetched_user.is_none() {
            if is_auth {
                let uuid = session_uuid.get();
                if let Some(uuid) = uuid {
                    spawn_local(async move {
                        let fetched = fetch_self(uuid).await.unwrap();
                        match fetched {
                            Ok(info) => {
                                set_fetched_user.set(Some(info));
                            }
                            Err(err) => {
                                logging::log!("{}", err);
                            }
                        }
                    })
                }
            }
        }
    });

    create_effect(move |_| {
        let is_auth = rw_is_authenticated.get();
        let session_id = session_uuid.get();

        if let Some(session_id) = session_id {
            if is_auth {
                let chats = create_resource(|| (), move |_| async move {
                    fetch_chats(session_id).await.unwrap()
                });
    
                create_effect(move |_| {
                    let chats = chats.get();
                    if let Some(chats) = chats {
                        set_fetched_chats.set(Some(chats));
                    }
                });
            }
        }
    });
    view! {
        <div class="flex min-w-[300px] flex-col h-full">
            <Suspense fallback=move || view! {
                <div class="flex flex-col h-screen p-4 space-y-4 overflow-auto">
                {
                    let mut to_view: Vec<_> = vec![];

                    for _ in 0..12 {
                        to_view.push(view!{<div class="flex-grow">{chat_placeholder()}</div>});
                    }
                    to_view
                }
                </div>
            }>
                // TODO: remove this fcking placeholder
                {session_uuid.get();}

                <div class="absolute z-10 w-[300px] left-0 top-0">
                    <div class="w-full min-h-[50px] flex flex-row items-center px-2 backdrop-blur-lg bg-opacity-50">
                        <input type="text" placeholder="Search"
                        class="w-full transition-theme px-4 py-2 bg-white/70 dark:bg-gray-700/70 text-black dark:text-white
                            rounded-xl shadow-md focus:outline-none focus:ring-2 focus:ring-sky-500 
                            focus:ring-opacity-50"/>
                    </div>
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                </div>

                <div class="flex-grow scroll-smooth overflow-auto pt-[50px] pb-[80px]">
                    {
                        move || {
                            match fetched_chats.get() {
                                Some(fetched) => {
                                    if !fetched.is_empty() {
                                        view! {
                                            <div>
                                                {fetched.iter().map(|chat| {
                                                    chat_view(&chat.name, &chat.last_message.as_ref().unwrap().message, &chat.last_message.as_ref().unwrap().name)
                                                }).collect::<Vec<_>>()}
                                            </div>
                                        }
                                    } else {
                                        view! {
                                            <div class="flex justify-center iterms-center">
                                                <h1 class="text-md font-bold">"No chats yet."</h1>
                                            </div>
                                        }
                                    }
                                },
                                None => view! {
                                    <div class="flex flex-col h-screen p-4 space-y-4 overflow-auto">
                                    {
                                        let mut to_view: Vec<_> = vec![];

                                        for _ in 0..12 {
                                            to_view.push(view!{<div class="flex-grow">{chat_placeholder()}</div>});
                                        }
                                        to_view
                                    }
                                    </div>
                                }
                            }
                        }
                    }
                </div>

                <div class="absolute w-[300px] bottom-0 left-0">
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                    <div class="w-full flex flex-row items-center px-2 h-[80px] backdrop-blur-lg bg-opacity-50">
                        <button on:click=move |_| {
                            set_show_menu(!show_menu.get())
                        } class="flex p-2 items-center hover:outline hover:outline-1 rounded-lg hover:outline-sky-500">
                            <div class="flex flex-row items-center">
                                <img class="w-10 h-10" src="default_avatar.png"/>
                                {move || match fetched_user.get() {
                                    Some(usr) => view!{<h1 class="text-xl ml-3 font-extrabold">{usr.data.name}</h1>},
                                    None => view!{<h1 class="text-xl ml-3 font-extrabold">
                                        <div class="text-center">
                                            <div role="status">
                                                <svg aria-hidden="true" class="inline w-8 h-8 text-gray-200 animate-spin dark:text-gray-600 fill-blue-600" viewBox="0 0 100 101" fill="none" xmlns="http://www.w3.org/2000/svg">
                                                    <path d="M100 50.5908C100 78.2051 77.6142 100.591 50 100.591C22.3858 100.591 0 78.2051 0 50.5908C0 22.9766 22.3858 0.59082 50 0.59082C77.6142 0.59082 100 22.9766 100 50.5908ZM9.08144 50.5908C9.08144 73.1895 27.4013 91.5094 50 91.5094C72.5987 91.5094 90.9186 73.1895 90.9186 50.5908C90.9186 27.9921 72.5987 9.67226 50 9.67226C27.4013 9.67226 9.08144 27.9921 9.08144 50.5908Z" fill="currentColor"/>
                                                    <path d="M93.9676 39.0409C96.393 38.4038 97.8624 35.9116 97.0079 33.5539C95.2932 28.8227 92.871 24.3692 89.8167 20.348C85.8452 15.1192 80.8826 10.7238 75.2124 7.41289C69.5422 4.10194 63.2754 1.94025 56.7698 1.05124C51.7666 0.367541 46.6976 0.446843 41.7345 1.27873C39.2613 1.69328 37.813 4.19778 38.4501 6.62326C39.0873 9.04874 41.5694 10.4717 44.0505 10.1071C47.8511 9.54855 51.7191 9.52689 55.5402 10.0491C60.8642 10.7766 65.9928 12.5457 70.6331 15.2552C75.2735 17.9648 79.3347 21.5619 82.5849 25.841C84.9175 28.9121 86.7997 32.2913 88.1811 35.8758C89.083 38.2158 91.5421 39.6781 93.9676 39.0409Z" fill="currentFill"/>
                                                </svg>
                                                <span class="sr-only">Loading...</span>
                                            </div>
                                        </div>
                                    </h1>}
                                }}
                            </div>
                        </button>
                        {move || if show_menu.get() {
                            view! {
                                <div class="transition-theme absolute bg-white dark:bg-slate-800 rounded-lg shadow-lg z-50 transition-all duration-300 ease-in-out" style="bottom: 80px;">
                                    <button class="block w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-slate-700 hover:rounded-lg">
                                        <div class="flex flex-row items-center">
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 mr-3">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z" />
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                                        </svg>
                                        "Settings"
                                        </div>
                                    </button>
                                    <button class="block w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:rounded-lg">
                                        <div class="flex flex-row items-center">
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 mr-3">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.963 0a9 9 0 1 0-11.963 0m11.963 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                                        </svg>
                                        "Profile"
                                        </div>
                                    </button>
                                    <button on:click=move |_| {
                                        set_cookies_auth(None);
                                        let id = session_uuid.get().unwrap();
                                        rw_is_authenticated.set(false);
                                        spawn_local(async move {
                                            reopen_session(id).await.unwrap();
                                        })
                                    } class="block w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:rounded-lg">
                                        <div class="flex flex-row items-center">
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 mr-3">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 9V5.25A2.25 2.25 0 0 1 10.5 3h6a2.25 2.25 0 0 1 2.25 2.25v13.5A2.25 2.25 0 0 1 16.5 21h-6a2.25 2.25 0 0 1-2.25-2.25V15m-3 0-3-3m0 0 3-3m-3 3H15" />
                                        </svg>
                                        "Log out"
                                        </div>
                                    </button>
                                </div>
                            }
                        } else {view!{<div></div>}}}
                        <div class="ml-auto w-15 h-15">
                            {ThemeToggler()}
                        </div>
                    </div>
                </div>
            </Suspense>
        </div>
    }
}
