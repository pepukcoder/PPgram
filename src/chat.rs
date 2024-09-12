use leptos::*;

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

fn message_view(from_me: bool, status: Status, text: String, time: String) -> impl IntoView {
    let alignment_class = if from_me { "ml-auto mr-2" } else { "ml-2 mr-auto" };

    let bg_class = if from_me {
        "bg-gradient-to-r from-sky-500/10 to-sky-700/10 dark:text-white"
    } else {
        "bg-white/50 dark:bg-slate-800/25"
    };

    view! {
        <div class=format!("w-max z-10 flex p-4 my-3 transition-theme rounded-2xl {} backdrop-blur-lg shadow-lg {}", bg_class, alignment_class)>
            <p class="text-black dark:text-gray-300 text-sm mr-5">
                {text}
            </p>
            <div class="absolute flex flex-row" style="bottom: 2px; right: 6px;">
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
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
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

                <div class="h-full overflow-auto flex flex-col-reverse backdrop-blur-lg">
                    {(0..50).map(|i| {
                        message_view(i % 2 == 0, Status::Sent, "Пепук Пидарасня.".into(), "21:59".into())
                    }).collect::<Vec<_>>()}
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