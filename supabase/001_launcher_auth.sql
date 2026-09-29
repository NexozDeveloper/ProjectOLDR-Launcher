create extension if not exists pgcrypto;

create table if not exists public.users (
    username text primary key,
    password_hash text not null,
    created_at timestamptz not null default now()
);

alter table public.users add column if not exists password_hash text;

do $$
begin
    if exists (
        select 1
        from information_schema.columns
        where table_schema = 'public'
          and table_name = 'users'
          and column_name = 'password'
    ) then
        execute $migration$
            update public.users
            set password_hash = crypt(password, gen_salt('bf', 12))
            where password_hash is null and password is not null
        $migration$;
    end if;
end
$$;

create unique index if not exists users_username_lower_idx
    on public.users (lower(username));

alter table public.users alter column password_hash set not null;
alter table public.users enable row level security;
revoke all on table public.users from anon, authenticated;

create table if not exists public.launcher_signup_limits (
    client_id uuid primary key,
    window_started_at timestamptz not null default now(),
    accounts_created integer not null default 0
        check (accounts_created >= 0)
);

alter table public.launcher_signup_limits enable row level security;
revoke all on table public.launcher_signup_limits from anon, authenticated;

create or replace function public.register_launcher_user(
    p_username text,
    p_password text,
    p_client_id uuid
)
returns jsonb
language plpgsql
security definer
set search_path = public, extensions
as $$
declare
    v_username text := lower(trim(p_username));
    v_window_started_at timestamptz;
    v_accounts_created integer;
begin
    if v_username is null or v_username = '' or length(v_username) > 32 then
        raise exception 'USERNAME_INVALID: Username invalide.' using errcode = '22023';
    end if;

    if p_password is null or length(p_password) < 6 or length(p_password) > 128 then
        raise exception 'PASSWORD_INVALID: Mot de passe invalide.' using errcode = '22023';
    end if;

    insert into public.launcher_signup_limits (client_id)
    values (p_client_id)
    on conflict (client_id) do nothing;

    select window_started_at, accounts_created
      into v_window_started_at, v_accounts_created
      from public.launcher_signup_limits
     where client_id = p_client_id
     for update;

    if now() >= v_window_started_at + interval '5 days' then
        update public.launcher_signup_limits
           set window_started_at = now(), accounts_created = 0
         where client_id = p_client_id;
        v_accounts_created := 0;
    end if;

    if v_accounts_created >= 5 then
        raise exception 'RATE_LIMIT: You have been rate limited. Try again in 5 days.'
            using errcode = 'P0001';
    end if;

    if exists (select 1 from public.users where lower(username) = v_username) then
        raise exception 'USERNAME_TAKEN: Username déjà utilisé.' using errcode = 'P0001';
    end if;

    insert into public.users (username, password_hash)
    values (v_username, crypt(p_password, gen_salt('bf', 12)));

    update public.launcher_signup_limits
       set accounts_created = accounts_created + 1
     where client_id = p_client_id;

    return jsonb_build_object('username', v_username);
end;
$$;

create or replace function public.authenticate_launcher_user(
    p_username text,
    p_password text
)
returns jsonb
language plpgsql
security definer
set search_path = public, extensions
as $$
declare
    v_username text := lower(trim(p_username));
begin
    if not exists (
        select 1
          from public.users
         where lower(username) = v_username
           and password_hash = crypt(p_password, password_hash)
    ) then
        raise exception 'INVALID_CREDENTIALS: Identifiants invalides.' using errcode = '28000';
    end if;

    return jsonb_build_object('username', v_username);
end;
$$;

revoke all on function public.register_launcher_user(text, text, uuid) from public;
revoke all on function public.authenticate_launcher_user(text, text) from public;
grant execute on function public.register_launcher_user(text, text, uuid) to anon;
grant execute on function public.authenticate_launcher_user(text, text) to anon;