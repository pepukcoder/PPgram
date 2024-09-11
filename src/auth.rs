use leptos::*;

use crate::{api::api::send_register, theme::ThemeToggler, types::AuthCredentials};

pub fn use_auth_creds() -> (Signal<Option<AuthCredentials>>, WriteSignal<Option<AuthCredentials>>) {
    use_context().expect("credentials to be defined")
}

#[component]
pub fn Auth() -> impl IntoView {
    let (show_register, set_show_register) = create_signal(false);

    let (name, set_name) = create_signal("".to_string());
    let (username, set_username) = create_signal("@".to_string());
    let (password, set_password) = create_signal("".to_string());

    let session_uuid = use_context::<Resource<(), u64>>().unwrap();
    

    let on_register_submit = move |ev: leptos::ev::SubmitEvent| {
        ev.prevent_default();

        logging::log!("{} {} {}", name.get(), username.get(), password.get());

        let session_uuid = session_uuid.get().unwrap();
        create_resource(|| (), {
            move |_| async move {
                send_register(session_uuid, "fdsfs".into(), "fdsfs".into(), "fdsfs".into()).await.unwrap()
            }
        });
    };

    let on_login_submit = move |ev: leptos::ev::SubmitEvent| {
        ev.prevent_default();
    
        
    };

    view! {
        <div class="flex justify-center items-center transition-theme h-screen text-black bg-white dark:text-white dark:bg-slate-900">
            <div class="absolute inset-0 z-0">
                <div class="blob-gradient"></div>
            </div>
            <div class="fixed inset-0 z-20 backdrop-blur-xl bg-gray-400/50 dark:bg-slate-900/50">
            </div>
            <div class="absolute z-50 bottom-0 left-0">
                {ThemeToggler()}
            </div>

            // <Suspense fallback=move || {
            //     view! {<p>fsdfs</p>}
            // }>
            //     {move || {reg_response.unwrap().get().unwrap().to_string()}}
            // </Suspense>

            <div class="transition-theme w-md z-50 p-8 bg-white dark:bg-slate-800 shadow-2xl rounded-lg border border-gray-200 dark:border-slate-700 backdrop-blur-lg bg-opacity-75">
                {
                    move || {if show_register.get() {
                        view! {
                            <div>
                            <button on:click=move |_| {
                                set_show_register.set(false)
                            } class="absolute top-0 left-0" style="margin-top: 1rem; margin-left: 1rem;">
                            <svg class="w-6 h-6 text-gray-800 dark:text-white" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 14 10">
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5H1m0 0 4 4M1 5l4-4"></path>
                            </svg>
                            </button>
                            <h1 class="text-2xl font-bold text-center mb-6">Sign Up</h1>
                            <form on:submit=on_register_submit action="#" method="POST" class="space-y-4">
                                <div>
                                    <label for="name" class="block text-sm font-medium">Name</label>
                                    <input on:input=move |ev| {
                                        set_name(event_target_value(&ev));
                                    } 
                                    type="name" id="name" name="name" required
                                        class="transition-theme w-full p-2 mt-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-slate-700 dark:border-slate-600 dark:focus:ring-sky-500"/>
                                </div>
                                <div class="flex items-center mt-1 border border-gray-300 rounded-md dark:bg-slate-700 dark:border-slate-600 focus-within:ring-2 focus-within:ring-blue-500 dark:focus-within:ring-sky-500">
                                    <span class="text-xl font-bold pl-2 pb-1 pr-1 pointer-events-none">@</span>
                                    <input on:input=move |ev| {
                                            set_username(event_target_value(&ev));
                                        } 
                                        type="text" 
                                        id="username" 
                                        name="username" 
                                        required
                                        class="transition-theme w-full pr-2 pt-2 pb-2 focus:outline-none bg-transparent border-none dark:bg-slate-700 dark:text-white"
                                    />
                                </div>
                                <div>
                                    <label for="password" class="block text-sm font-medium">Password</label>
                                    <input on:input=move |ev| {
                                        set_password(event_target_value(&ev));
                                    } type="password" id="password" name="password" required
                                        class="transition-theme w-full p-2 mt-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-sky-500 dark:bg-slate-700 dark:border-slate-600 dark:focus:ring-sky-500"/>
                                </div>
                                <div class="flex justify-between items-center">
                                    <div class="flex items-center mr-3">
                                        <div class="flex items-center">
                                            <input checked id="checked-checkbox" type="checkbox" value="" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 focus:ring-2 dark:bg-gray-700 dark:border-gray-600"/>
                                            <label for="checked-checkbox" class="ms-2 text-sm font-medium text-gray-900 dark:text-gray-300">Remember me</label>
                                        </div>
                                    </div>
                                </div>
                                
                                <button value="Submit" type="submit"
                                    class="w-full py-2 px-4 bg-blue-600 text-white font-semibold rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-sky-600 dark:hover:bg-sky-700 dark:focus:ring-sky-500">Sign Up</button>
                            </form>
                            </div>
                        }
                    } else {
                        view! {
                            <div>
                            <h1 class="text-2xl font-bold text-center mb-6">Sign In</h1>
                            <form on:submit=on_login_submit action="#" method="POST" class="space-y-4">
                                <div>
                                    <label for="username" class="block text-sm font-medium">Username</label>
                                    <input type="username" id="username" name="username" required
                                        class="transition-theme w-full p-2 mt-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-slate-700 dark:border-slate-600 dark:focus:ring-sky-500"/>
                                </div>
                                <div>
                                    <label for="password" class="block text-sm font-medium">Password</label>
                                    <input type="password" id="password" name="password" required
                                        class="transition-theme w-full p-2 mt-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-sky-500 dark:bg-slate-700 dark:border-slate-600 dark:focus:ring-sky-500"/>
                                </div>
                                <div class="flex justify-between items-center">
                                    <div class="flex items-center mr-3">
                                        <div class="flex items-center">
                                            <input checked id="checked-checkbox" type="checkbox" value="" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 focus:ring-2 dark:bg-gray-700 dark:border-gray-600"/>
                                            <label for="checked-checkbox" class="ms-2 text-sm font-medium text-gray-900 dark:text-gray-300">Remember me</label>
                                        </div>
                                    </div>
                                    <button on:click=move |_| {
                                        set_show_register.set(true);
                                    } 
                                    class="text-sm text-blue-500 hover:underline dark:text-sky-400">{"Don't have an account?"}</button>
                                </div>
                                
                                <button type="submit"
                                    class="w-full py-2 px-4 bg-blue-600 text-white font-semibold rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-sky-600 dark:hover:bg-sky-700 dark:focus:ring-sky-500">Sign In</button>
                            </form>
                            </div>
                        }
                    }}
                }
            </div>
        </div>
    }
}