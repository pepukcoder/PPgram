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
                <svg class="w-12 h-12" viewBox="0 0 1024 1024" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path fill="#4B5563" d="M819.2 729.088V757.76c0 33.792-27.648 61.44-61.44 61.44H266.24c-33.792 0-61.44-27.648-61.44-61.44v-28.672c0-74.752 87.04-119.808 168.96-155.648 3.072-1.024 5.12-2.048 8.192-4.096 6.144-3.072 13.312-3.072 19.456 1.024C434.176 591.872 472.064 604.16 512 604.16c39.936 0 77.824-12.288 110.592-32.768 6.144-4.096 13.312-4.096 19.456-1.024 3.072 1.024 5.12 2.048 8.192 4.096 81.92 34.816 168.96 79.872 168.96 154.624z"/>
                    <path fill="#4B5563" d="M359.424 373.76a168.96 152.576 90 1 0 305.152 0 168.96 152.576 90 1 0-305.152 0Z"/>
                </svg>
                <div>
                    <h1 class="text-xl text-left font-extrabold">{title}</h1>
                    <p class="text-sm text-gray-600 dark:text-gray-400"><span class="text-sm font-bold text-gray-500 dark:text-gray-200">{last_message_sender}" "</span> {last_message}</p>
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
                        <input type="text" placeholder="Search..." 
                        class="w-full px-4 py-2 bg-white/70 dark:bg-gray-700/70 text-black dark:text-white 
                            rounded-full shadow-md focus:outline-none focus:ring-2 focus:ring-indigo-500 
                            focus:ring-opacity-50"/>
                    </div>
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                </div>

                <div class="flex-grow scroll-smooth overflow-auto pt-[50px] pb-[50px]">
                    {(0..50).map(|i| {
                        chat_view(&"Pepuk".into(), &"Pidarasina ebanaya".into(), &"Me:".into())
                    }).collect::<Vec<_>>()}
                </div>

                <div class="absolute w-[300px] inset-x-0 bottom-0 left-0">
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                    <div class="w-full flex flex-row items-center px-2 backdrop-blur-lg bg-opacity-50">
                        <h1 class="text-center font-extrabold align-center">PashtetosAlpha</h1>
                        <div class="ml-auto">
                            {ThemeToggler()}
                        </div>
                    </div>
                </div>
            </Suspense>
        </div>
    }
}