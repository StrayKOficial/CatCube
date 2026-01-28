-- Run this in your Supabase SQL Editor to create the profiles table

create table public.profiles (
  id uuid references auth.users not null primary key,
  username text unique,
  body_type int default 0,
  hair_style int default 0,
  shirt_color text default '#CC3333',
  pants_color text default '#264073',
  skin_color text default '#FFD9B8',
  created_at timestamp with time zone default timezone('utc'::text, now()) not null
);

-- Enable Row Level Security (RLS)
alter table public.profiles enable row level security;

-- Create Policy: Public can view profiles
create policy "Public profiles are viewable by everyone."
  on profiles for select
  using ( true );

-- Create Policy: Users can insert their own profile
create policy "Users can insert their own profile."
  on profiles for insert
  with check ( auth.uid() = id );

-- Create Policy: Users can update own profile
create policy "Users can update own profile."
  on profiles for update
  using ( auth.uid() = id );

-- Setup trigger to create profile on signup (Optional but good)
-- For now, we will handle creation in the C# Launcher logic
