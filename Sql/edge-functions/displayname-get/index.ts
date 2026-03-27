import { createClient } from "npm:@supabase/supabase-js@2";

type GetRequest = {
  user_id?: string;
};

type GetResponse = {
  ok: boolean;
  display_name?: string;
  reason?: string;
};

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SUPABASE_ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY")!;

Deno.serve(async (req) => {
  let body: GetRequest | null = null;
  try {
    body = await req.json();
  } catch {
    body = null;
  }

  const userId = body?.user_id?.trim();
  if (!userId) {
    return new Response(
      JSON.stringify({ ok: false, reason: "user_id_empty" } satisfies GetResponse),
      { status: 400, headers: { "Content-Type": "application/json" } },
    );
  }

  // 공개 함수: anon key로 display_names에서 조회 (RLS: select public)
  const client = createClient(SUPABASE_URL, SUPABASE_ANON_KEY);
  const res = await client
    .from("display_names")
    .select("display_name")
    .eq("user_id", userId)
    .limit(1)
    .maybeSingle();

  if (res.error) {
    return new Response(
      JSON.stringify({ ok: false, reason: res.error.message } satisfies GetResponse),
      { status: 500, headers: { "Content-Type": "application/json" } },
    );
  }

  return new Response(
    JSON.stringify({ ok: true, display_name: res.data?.display_name ?? "" } satisfies GetResponse),
    { headers: { "Content-Type": "application/json" } },
  );
});

