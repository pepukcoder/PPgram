use std::{borrow::Cow, default};

use leptos::*;
use rand::Rng;

use crate::theme::ThemeToggler;

fn chat_placeholder() -> impl IntoView {
    let mut rng = rand::thread_rng();
    let name_percent = rng.gen_range(30..80);
    let message_percent = rng.gen_range(40..100);

    view!{
        <div class="flex p-2 items-start space-x-4 w-full">
            <div class="animate-pulse rounded-full bg-gray-300 dark:bg-slate-700 h-10 w-10 outline outline-2 outline-gray-500 dark:outline-slate-500"></div>
            <div class="flex flex-col w-[80%]">
                <div class="animate-pulse h-4 bg-gray-300 dark:bg-slate-700 rounded outline outline-2 outline-gray-500 dark:outline-slate-500" style={format!("width: {}%", name_percent)}></div>
                <div class="animate-pulse mt-2 h-4 bg-gray-300 dark:bg-slate-700 rounded outline outline-2 outline-gray-500 dark:outline-slate-500" style={format!("width: {}%", message_percent)}></div>
            </div>
        </div>
    }
}

fn chat_view(title: &String, last_message: &String, last_message_sender: &String) -> impl IntoView
{
    view! {
        <div class="flex flex-col rounded-lg">
            <button on:click=move |_| {
                logging::log!("Huy")
            } 
            class="flex items-center p-2 space-x-3 hover:bg-gray-100 dark:hover:bg-gray-800 justify-start">
                <img class="w-12 h-12" src="default_avatar.png"/>
                <div>
                    <h1 class="text-xl text-left font-extrabold">{title}</h1>
                    <p class="text-sm text-gray-600 dark:text-gray-400 truncate w-56"><span class="text-sm font-bold text-gray-500 dark:text-gray-200">{last_message_sender}" "</span> {last_message}</p>
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

    view!{
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
                    {(0..50).map(|i| {
                        chat_view(&"Pepuk Chmonya".into(), &"Ооо владимир владимирович да... О да В.В.".into(), &"Pepuk:".into())
                    }).collect::<Vec<_>>()}
                </div>

                <div class="absolute w-[300px] bottom-0 left-0">
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                    <div class="w-full flex flex-row items-center px-2 h-[80px] backdrop-blur-lg bg-opacity-50">
                        <button class="flex p-2 items-center hover:outline hover:outline-2 rounded-lg hover:outline-sky-500">
                            <div class="flex flex-row items-center">
                                <img class="w-10 h-10" src="default_avatar.png"/>
                                <h1 class="text-xl ml-3 font-extrabold">PashtetosAlpha</h1>
                            </div>
                        </button>
                        <div class="ml-auto w-15 h-15">
                            {ThemeToggler()}
                        </div>
                    </div>
                </div>
            </Suspense>
        </div>
    }
}