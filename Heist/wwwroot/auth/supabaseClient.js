import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';

const SUPABASE_URL = 'https://zozoarcafzznjmztyoyt.supabase.co';   // Project URL
const SUPABASE_ANON_KEY = 'sb_publishable_cuIMzx7FG07uj_6D2bOYbA_0FWjQmRY';          // anon public key

export const supabase = createClient(SUPABASE_URL, SUPABASE_ANON_KEY);