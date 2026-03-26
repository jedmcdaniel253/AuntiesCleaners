import { serve } from "https://deno.land/std@0.177.0/http/server.ts";

function jsonResponse(body: Record<string, unknown>, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    },
  });
}

serve(async (req: Request) => {
  // Handle CORS preflight
  if (req.method === "OPTIONS") {
    return new Response(null, {
      status: 204,
      headers: {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Methods": "POST, OPTIONS",
        "Access-Control-Allow-Headers": "Authorization, Content-Type, apikey, x-client-info",
      },
    });
  }

  if (req.method !== "POST") {
    return jsonResponse({ error: "Method not allowed" }, 405);
  }

  const openaiKey = Deno.env.get("OPENAI_API_KEY");
  if (!openaiKey) {
    return jsonResponse({ error: "OPENAI_API_KEY not configured" }, 500);
  }

  try {
    const { image } = await req.json();
    if (!image) {
      return jsonResponse({ error: "Missing required field: image (base64)" }, 400);
    }

    console.log("Calling OpenAI with image size:", image.length, "chars");
    const response = await fetch("https://api.openai.com/v1/chat/completions", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${openaiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        model: "gpt-4o-mini",
        messages: [
          {
            role: "user",
            content: [
              {
                type: "text",
                text: `Extract the business name and total amount from this receipt image. Return ONLY valid JSON with this exact format, no other text:
{"businessName": "Store Name", "amount": 12.34}
If you cannot determine a field, use empty string for businessName and 0 for amount.`,
              },
              {
                type: "image_url",
                image_url: { url: `data:image/jpeg;base64,${image}` },
              },
            ],
          },
        ],
        max_tokens: 100,
      }),
    });

    if (!response.ok) {
      const err = await response.text();
      console.error("OpenAI API error:", response.status, err);
      return jsonResponse({ error: `OpenAI API error: ${err}` }, 500);
    }

    const data = await response.json();
    console.log("OpenAI response:", JSON.stringify(data.choices?.[0]?.message?.content));
    const content = data.choices?.[0]?.message?.content?.trim() ?? "";

    // Parse the JSON from the response
    const jsonMatch = content.match(/\{[\s\S]*\}/);
    if (!jsonMatch) {
      return jsonResponse({ businessName: "", amount: 0, rawText: content });
    }

    const parsed = JSON.parse(jsonMatch[0]);
    return jsonResponse({
      businessName: parsed.businessName || "",
      amount: typeof parsed.amount === "number" ? parsed.amount : parseFloat(parsed.amount) || 0,
      rawText: content,
    });
  } catch (error) {
    console.error("OCR function error:", error);
    return jsonResponse({ error: `OCR failed: ${String(error)}` }, 500);
  }
});
