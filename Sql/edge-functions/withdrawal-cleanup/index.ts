import { createClient } from "npm:@supabase/supabase-js@2";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;

Deno.serve(async () => {
  const adminClient = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);
  const nowIso = new Date().toISOString();

  const dueRes = await adminClient
    .from("profiles")
    .select("account_id, withdrawn_at")
    .not("withdrawn_at", "is", null)
    .lte("withdrawn_at", nowIso)
    .limit(500);

  if (dueRes.error) {
    return new Response(JSON.stringify({ ok: false, reason: dueRes.error.message }), {
      status: 500,
      headers: { "Content-Type": "application/json" },
    });
  }

  const rows = dueRes.data ?? [];
  let deletedCount = 0;

  for (const row of rows) {
    const accountId = row.account_id as string | null;
    if (!accountId) continue;

    await adminClient.from("account_closures").upsert(
      {
        user_id: accountId,
        account_id: accountId,
        closed_at: new Date().toISOString(),
        note: "withdrawal_cleanup",
      },
      { onConflict: "user_id" },
    );

    const deleteRes = await adminClient.auth.admin.deleteUser(accountId, false);
    if (!deleteRes.error) deletedCount++;
  }

  return new Response(
    JSON.stringify({
      ok: true,
      scanned: rows.length,
      deleted: deletedCount,
    }),
    { headers: { "Content-Type": "application/json" } },
  );
});

