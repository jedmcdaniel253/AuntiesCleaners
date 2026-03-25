import { serve } from "https://deno.land/std@0.177.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const REDIRECT_URL =
  "https://jedmcdaniel253.github.io/AuntiesCleaners/set-password";

export function buildInviteEmailHtml(
  name: string,
  recoveryLink: string
): string {
  return `
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
      <h2 style="color: #333;">Welcome to Auntie's Cleaners!</h2>
      <p>Hi ${name},</p>
      <p>You've been invited to join the Auntie's Cleaners app. To get started, please set your password by clicking the link below:</p>
      <p style="margin: 24px 0;">
        <a href="${recoveryLink}" style="background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">Set Your Password</a>
      </p>
      <p>Or copy and paste this link into your browser:</p>
      <p style="word-break: break-all; color: #666;">${recoveryLink}</p>
      <p>If you didn't expect this invitation, you can safely ignore this email.</p>
      <p style="color: #999; font-size: 12px; margin-top: 32px;">— Auntie's Cleaners Team</p>
    </div>
  `;
}

export function jsonResponse(body: Record<string, unknown>, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

export async function sendInviteEmail(
  resendApiKey: string,
  email: string,
  name: string,
  recoveryLink: string
): Promise<{ emailSent: boolean; warning?: string }> {
  try {
    const html = buildInviteEmailHtml(name, recoveryLink);
    const resendResponse = await fetch("https://api.resend.com/emails", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${resendApiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        from: "Auntie's Cleaners <noreply@auntiecleaners.com>",
        to: [email],
        subject: "You're invited to Auntie's Cleaners",
        html,
      }),
    });

    if (!resendResponse.ok) {
      const details = await resendResponse.text();
      return {
        emailSent: false,
        warning: `Email delivery failed: ${details}`,
      };
    }

    return { emailSent: true };
  } catch (error) {
    return {
      emailSent: false,
      warning: `Email delivery failed: ${String(error)}`,
    };
  }
}

export async function generateRecoveryLink(
  supabaseAdmin: ReturnType<typeof createClient>,
  email: string
): Promise<{ link: string | null; error?: string }> {
  const { data, error } = await supabaseAdmin.auth.admin.generateLink({
    type: "recovery",
    email,
    options: { redirectTo: REDIRECT_URL },
  });

  if (error || !data?.properties?.action_link) {
    return {
      link: null,
      error: error?.message || "Failed to generate recovery link",
    };
  }

  return { link: data.properties.action_link };
}

export async function handleInvite(
  supabaseAdmin: ReturnType<typeof createClient>,
  resendApiKey: string,
  body: Record<string, unknown>
): Promise<Response> {
  const { name, email, role, workerId } = body as {
    name?: string;
    email?: string;
    role?: string;
    workerId?: string;
  };

  // Validate required fields
  const missing: string[] = [];
  if (!name) missing.push("name");
  if (!email) missing.push("email");
  if (!role) missing.push("role");
  if (missing.length > 0) {
    return jsonResponse(
      { success: false, error: `Missing required fields: ${missing.join(", ")}` },
      400
    );
  }

  // 1. Create auth user
  const { data: authData, error: authError } =
    await supabaseAdmin.auth.admin.createUser({
      email,
      email_confirm: true,
      user_metadata: { name },
    });

  if (authError) {
    if (
      authError.message?.toLowerCase().includes("already") ||
      authError.message?.toLowerCase().includes("duplicate") ||
      authError.status === 422
    ) {
      return jsonResponse(
        { success: false, error: "Email already registered" },
        409
      );
    }
    return jsonResponse(
      { success: false, error: authError.message },
      500
    );
  }

  const authUserId = authData.user.id;

  // 2. Insert user_profiles row
  const profileRow: Record<string, unknown> = {
    auth_user_id: authUserId,
    name,
    email,
    role,
    is_active: true,
  };
  if (workerId) {
    profileRow.worker_id = workerId;
  }

  const { data: profileData, error: profileError } = await supabaseAdmin
    .from("user_profiles")
    .insert(profileRow)
    .select("id")
    .single();

  if (profileError) {
    // Rollback: delete the auth user we just created
    await supabaseAdmin.auth.admin.deleteUser(authUserId);
    return jsonResponse(
      { success: false, error: "Profile creation failed" },
      500
    );
  }

  // 3. Generate recovery link
  const { link: recoveryLink, error: linkError } =
    await generateRecoveryLink(supabaseAdmin, email!);

  if (!recoveryLink) {
    // Account and profile were created, but link generation failed
    return jsonResponse({
      success: true,
      userId: authUserId,
      profileId: profileData.id,
      emailSent: false,
      warning: linkError || "Failed to generate recovery link",
    });
  }

  // 4. Send invite email
  const emailResult = await sendInviteEmail(
    resendApiKey,
    email!,
    name!,
    recoveryLink
  );

  const response: Record<string, unknown> = {
    success: true,
    userId: authUserId,
    profileId: profileData.id,
    emailSent: emailResult.emailSent,
  };
  if (emailResult.warning) {
    response.warning = emailResult.warning;
  }

  return jsonResponse(response);
}

export async function handleResend(
  supabaseAdmin: ReturnType<typeof createClient>,
  resendApiKey: string,
  body: Record<string, unknown>
): Promise<Response> {
  const { email, name } = body as { email?: string; name?: string };

  if (!email) {
    return jsonResponse(
      { success: false, error: "Missing required fields: email" },
      400
    );
  }

  // Generate a new recovery link
  const { link: recoveryLink, error: linkError } =
    await generateRecoveryLink(supabaseAdmin, email);

  if (!recoveryLink) {
    return jsonResponse(
      { success: false, error: linkError || "Failed to generate recovery link" },
      500
    );
  }

  // Send invite email
  const displayName = name || email;
  const emailResult = await sendInviteEmail(
    resendApiKey,
    email,
    displayName,
    recoveryLink
  );

  const response: Record<string, unknown> = {
    success: true,
    emailSent: emailResult.emailSent,
  };
  if (emailResult.warning) {
    response.warning = emailResult.warning;
  }

  return jsonResponse(response);
}

serve(async (req: Request) => {
  if (req.method !== "POST") {
    return jsonResponse({ success: false, error: "Method not allowed" }, 405);
  }

  // Validate environment
  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  if (!supabaseUrl || !serviceRoleKey) {
    return jsonResponse(
      { success: false, error: "Server configuration error" },
      500
    );
  }

  const resendApiKey = Deno.env.get("RESEND_API_KEY");

  // Validate authorization header
  const authHeader = req.headers.get("Authorization");
  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    return jsonResponse({ success: false, error: "Unauthorized" }, 401);
  }

  // Verify the caller's JWT using a client with the anon key extracted from the auth header
  const token = authHeader.replace("Bearer ", "");
  const supabaseAnonKey = Deno.env.get("SUPABASE_ANON_KEY") || "";
  const supabaseAuth = createClient(supabaseUrl, supabaseAnonKey, {
    global: { headers: { Authorization: `Bearer ${token}` } },
  });

  const {
    data: { user },
    error: userError,
  } = await supabaseAuth.auth.getUser(token);

  if (userError || !user) {
    return jsonResponse({ success: false, error: "Unauthorized" }, 401);
  }

  // Create admin client for privileged operations
  const supabaseAdmin = createClient(supabaseUrl, serviceRoleKey);

  try {
    const body = await req.json();
    const action = body.action || "invite";

    if (action === "invite") {
      return await handleInvite(supabaseAdmin, resendApiKey || "", body);
    } else if (action === "resend") {
      return await handleResend(supabaseAdmin, resendApiKey || "", body);
    } else {
      return jsonResponse(
        { success: false, error: `Unknown action: ${action}` },
        400
      );
    }
  } catch (error) {
    return jsonResponse(
      { success: false, error: "Internal server error", message: String(error) },
      500
    );
  }
});
