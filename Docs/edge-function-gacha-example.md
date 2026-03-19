# Edge Function 예시: 뽑기 결과 서버 계산

목표: Unity 클라이언트는 "뽑기 요청"만 보내고, 결과 계산은 서버에서 수행합니다.

## 1) Supabase Edge Function (예시)

`supabase/functions/gacha-draw/index.ts`:

```ts
import { createClient } from "jsr:@supabase/supabase-js@2";

type DrawRequest = {
  bannerId: string;
  drawCount: number;
  seed?: number;
};

function pickItemByWeight(randomValue: number) {
  // 예시 가중치 테이블
  if (randomValue < 0.01) return { id: "legendary_001", rarity: "legendary" };
  if (randomValue < 0.10) return { id: "epic_001", rarity: "epic" };
  return { id: "common_001", rarity: "common" };
}

Deno.serve(async (request: Request) => {
  if (request.method !== "POST") {
    return new Response(JSON.stringify({ error: "method_not_allowed" }), { status: 405 });
  }

  const authHeader = request.headers.get("Authorization");
  if (!authHeader) {
    return new Response(JSON.stringify({ error: "unauthorized" }), { status: 401 });
  }

  const supabaseUrl = Deno.env.get("SUPABASE_URL")!;
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
  const supabase = createClient(supabaseUrl, serviceRoleKey, {
    global: { headers: { Authorization: authHeader } },
  });

  const { data: userData, error: userError } = await supabase.auth.getUser();
  if (userError || !userData?.user) {
    return new Response(JSON.stringify({ error: "invalid_user" }), { status: 401 });
  }

  const body = (await request.json()) as DrawRequest;
  const bannerId = body.bannerId;
  const drawCount = Math.max(1, Math.min(body.drawCount ?? 1, 10));

  // TODO: 재화 차감/검증 로직(트랜잭션 또는 RPC) 추가

  const now = Date.now();
  const rewards = Array.from({ length: drawCount }).map((_, index) => {
    const pseudoRandom = ((now + index * 7919) % 10000) / 10000;
    return pickItemByWeight(pseudoRandom);
  });

  return new Response(
    JSON.stringify({
      bannerId,
      drawCount,
      rewards,
      serverTime: new Date().toISOString(),
    }),
    { status: 200, headers: { "Content-Type": "application/json" } },
  );
});
```

배포:

```bash
supabase functions deploy gacha-draw
```

## 2) Unity 호출 예시 (현재 SDK)

```csharp
[System.Serializable]
public class DrawRequest
{
    public string bannerId;
    public int drawCount;
}

[System.Serializable]
public class DrawReward
{
    public string id;
    public string rarity;
}

[System.Serializable]
public class DrawResponse
{
    public string bannerId;
    public int drawCount;
    public DrawReward[] rewards;
    public string serverTime;
}

// 로그인 세션 필요
var payload = new DrawRequest
{
    bannerId = "normal_banner_001",
    drawCount = 10
};

var result = await Supabase.InvokeFunctionAsync<DrawResponse>("gacha-draw", payload);
if (result.IsSuccess)
{
    var draw = result.Data;
    // draw.rewards 반영
}
else
{
    UnityEngine.Debug.LogError("Draw failed: " + result.ErrorMessage);
}
```

주의:
- Edge Function 응답이 루트 객체(JSON이 `{`로 시작)인 경우가 가장 깔끔합니다.
- 만약 루트가 배열(JSON이 `[`로 시작)로 오더라도 SDK가 첫 번째 원소를 객체처럼 파싱하도록 처리해 줍니다. (단, 성능/가독성 측면에서는 객체 루트를 권장)

## 3) 권장 보안 체크

- 클라이언트에서 결과 계산 금지 (서버에서만 확률/시드/테이블 계산)
- 사용자 재화 검증/차감 로직은 서버 함수 내부에서 처리
- 중복 요청 방지를 위해 request id(멱등 키) 고려
- 로그 테이블(`gacha_draw_logs`)에 user_id, banner_id, rewards, created_at 기록 권장

