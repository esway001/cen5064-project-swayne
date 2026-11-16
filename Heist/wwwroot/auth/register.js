import { supabase } from './supabaseClient.js';

// 16+ chars, with upper, lower, number, symbol
const PASSWORD_RE = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{16,}$/;

const modalEl = document.getElementById('registerModal');
const form = document.getElementById('registerForm');
const statusEl = document.getElementById('registerStatus');

function say(msg, ok = false) {
    statusEl.textContent = msg;
    statusEl.className = `small ${ok ? 'text-success' : 'text-danger'}`;
}

modalEl.addEventListener('show.bs.modal', () => say(''));

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    say('');

    const email = document.getElementById('regEmail').value.trim();
    const username = document.getElementById('regUsername').value.trim();
    const password = document.getElementById('regPassword').value;

    // here password validation
    if (!PASSWORD_RE.test(password)) {
        say('Password needs 16+ characters with upper, lower, number, and symbol.');
        return;
    }

    // user check
    const { data: available, error: rpcErr } =
        await supabase.rpc('username_available', { name: username });
    if (rpcErr) { say('Could not check username, try again.'); return; }
    
    if (!available) { say('That username is taken.'); return; }

    // make account
    const { error } = await supabase.auth.signUp({
        email, password,
        options: { data: { username } }   // trigger reads this into profiles
    });
    if (error) { say(error.message); return; }

    // email already exists
    say('Account created — you can log in now.', true);
    form.reset();
    setTimeout(() => bootstrap.Modal.getOrCreateInstance(modalEl).hide(), 1200);
});