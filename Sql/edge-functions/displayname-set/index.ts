import { createClient } from "npm:@supabase/supabase-js@2";

type SetRequest = {
  display_name?: string;
  user_id?: string; // stable player id (oauth sub etc) supplied by client/session
};

type SetResponse = {
  ok: boolean;
  reason?: string;
};

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SUPABASE_ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY")!;
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;

function normalize(name: string): string {
  return name.trim();
}

Deno.serve(async (req) => {
  const authHeader = req.headers.get("Authorization") ?? "";
  const jwt = authHeader.startsWith("Bearer ")
    ? authHeader.slice("Bearer ".length)
    : "";

  if (!jwt) {
    return new Response(
      JSON.stringify({ ok: false, reason: "missing_jwt" } satisfies SetResponse),
      { status: 401, headers: { "Content-Type": "application/json" } },
    );
  }

  let body: SetRequest | null = null;
  try {
    body = await req.json();
  } catch {
    body = null;
  }

  const rawName = body?.display_name ?? "";
  const displayName = normalize(rawName);
  if (!displayName) {
    return new Response(
      JSON.stringify({ ok: false, reason: "display_name_empty" } satisfies SetResponse),
      { status: 400, headers: { "Content-Type": "application/json" } },
    );
  }
  if (displayName.length > 64) {
    return new Response(
      JSON.stringify({ ok: false, reason: "display_name_too_long" } satisfies SetResponse),
      { status: 400, headers: { "Content-Type": "application/json" } },
    );
  }

  const userClient = createClient(SUPABASE_URL, SUPABASE_ANON_KEY, {
    global: { headers: { Authorization: `Bearer ${jwt}` } },
  });

  const userRes = await userClient.auth.getUser();
  const user = userRes.data.user;
  if (!user) {
    return new Response(
      JSON.stringify({ ok: false, reason: "user_not_found" } satisfies SetResponse),
      { status: 401, headers: { "Content-Type": "application/json" } },
    );
  }

  const playerUserId = (body?.user_id ?? "").trim() || user.id;

  // 1) upsert claim row for this account
  // Uniqueness is enforced by DB unique index on lower(trim(display_name)).
  const upsert = await userClient
    .from("display_names")
    .upsert(
      { account_id: user.id, user_id: playerUserId, display_name: displayName, updated_at: new Date().toISOString() },
      { onConflict: "account_id" },
    );

  if (upsert.error) {
    const msg = upsert.error.message ?? "display_name_upsert_failed";
    const reason = msg.toLowerCase().includes("duplicate") || msg.toLowerCase().includes("unique")
      ? "display_name_taken"
      : msg;
    return new Response(
      JSON.stringify({ ok: false, reason } satisfies SetResponse),
      { status: reason === "display_name_taken" ? 409 : 500, headers: { "Content-Type": "application/json" } },
    );
  }

  // 2) sync auth user metadata
  const upd = await userClient.auth.updateUser({ data: { displayName } });
  if (upd.error) {
    return new Response(
      JSON.stringify({ ok: false, reason: upd.error.message } satisfies SetResponse),
      { status: 500, headers: { "Content-Type": "application/json" } },
    );
  }

  return new Response(JSON.stringify({ ok: true } satisfies SetResponse), {
    headers: { "Content-Type": "application/json" },
  });
});

