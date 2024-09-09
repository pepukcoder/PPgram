use leptos::*;

use crate::api::api::send_auth;

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


#[component]
pub fn Chat() -> impl IntoView {
    let session_uuid = use_context::<Resource<(), u64>>().unwrap();

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
                    <div class="w-full max-h-[50px] flex flex-row items-center px-2 backdrop-blur-lg bg-opacity-50">
                        <div class="flex flex-col p-1">
                            <h1 class="font-extrabold text-md">Pepuk</h1>
                            <h1 class="text-sm text-gray-400 dark:text-gray-500">Last seen online at 12:59</h1>
                        </div>
                    </div>
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                </div>

                <div class="h-full overflow-auto">
                </div>

                <div class="relative w-fullleft-300 bottom-0">
                    <hr class="border-gray-600 opacity-25 dark:border-gray-600"/>
                    <div class="flex flex-row  min-h-[75px] items-center px-2 backdrop-blur-lg bg-opacity-50">
                        <div class="flex flex-col w-full my-4">
                            <input 
                                rows="1" type="text" placeholder="Your message" 
                                class="w-full transition-theme px-6 py-3 bg-white/70 dark:bg-gray-700/70 text-black dark:text-white 
                                rounded-xl shadow-2xl focus:ring-2 focus:ring-sky-500 focus:ring-opacity-50"/>
                        </div>
                    </div>
                </div>
            </div>
        </Suspense>
    }
}