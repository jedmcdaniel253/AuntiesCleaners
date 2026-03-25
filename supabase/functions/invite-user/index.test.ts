// deno-lint-ignore-file no-explicit-any
import {
  assertEquals,
  assertStringIncludes,
} from "https://deno.land/std@0.177.0/testing/asserts.ts";
import fc from "https://esm.sh/fast-check@3.15.0";
import {
  handleInvite,
  handleResend,
  buildInviteEmailHtml,
} from "./index.ts";

// ── Generators ──────────────────────────────────────────────────────────────

const VALID_ROLES = ["Worker", "Manager", "Boss", "Admin"] as const;

const nonEmptyName = fc.string({ minLength: 1, maxLength: 60 }).filter((s) => s.trim().length > 0);

const validEmail = fc
  .tuple(
    fc.string({ minLength: 1, maxLength: 20 }).filter((s) => /^[a-zA-Z0-9]+$/.test(s)),
    fc.string({ minLength: 2, maxLength: 10 }).filter((s) => /^[a-zA-Z]+$/.test(s))
  )
  .map(([local, domain]) => `${local}@${domain}.com`);

const validRole = fc.constantFrom(...VALID_ROLES);

const optionalWorkerId = fc.option(fc.uuid(), { nil: undefined });

const validInvitePayload = fc.record({
  name: nonEmptyName,
  email: validEmail,
  role: validRole,
  workerId: optionalWorkerId,
});

const validUrl = fc
  .tuple(
    fc.constantFrom("https://example.com", "https://app.test.io", "https://my-site.dev"),
    fc.string({ minLength: 4, maxLength: 30 }).filter((s) => /^[a-zA-Z0-9/\-_]+$/.test(s))
  )
  .map(([base, path]) => `${base}/${path}`);

// ── Mock Helpers ────────────────────────────────────────────────────────────

interface MockConfig {
  createUserResult?: { data: any; error: any };
  deleteUserResult?: { data: any; error: any };
  insertResult?: { data: any; error: any };
  generateLinkResult?: { data: any; error: any };
}

interface CallLog {
  createUser: any[];
  deleteUser: any[];
  insert: any[];
  generateLink: any[];
}

function createMockSupabaseAdmin(config: MockConfig = {}): { client: any; calls: CallLog } {
  const calls: CallLog = {
    createUser: [],
    deleteUser: [],
    insert: [],
    generateLink: [],
  };

  const defaultAuthUserId = crypto.randomUUID();

  const client = {
    auth: {
      admin: {
        createUser: async (params: any) => {
          calls.createUser.push(params);
          return (
            config.createUserResult ?? {
              data: { user: { id: defaultAuthUserId } },
              error: null,
            }
          );
        },
        deleteUser: async (id: string) => {
          calls.deleteUser.push(id);
          return config.deleteUserResult ?? { data: {}, error: null };
        },
        generateLink: async (params: any) => {
          calls.generateLink.push(params);
          return (
            config.generateLinkResult ?? {
              data: {
                properties: {
                  action_link: "https://example.com/recovery-link",
                },
              },
              error: null,
            }
          );
        },
      },
    },
    from: (_table: string) => {
      return {
        insert: (row: any) => {
          calls.insert.push(row);
          return {
            select: (_cols: string) => ({
              single: async () =>
                config.insertResult ?? {
                  data: { id: crypto.randomUUID() },
                  error: null,
                },
            }),
          };
        },
      };
    },
  };

  return { client, calls };
}


// ── Property 1: Invite creates auth user and matching profile ───────────────
// Feature: user-invite-flow, Property 1: Invite creates auth user and matching profile
// **Validates: Requirements 1.1, 1.2**

Deno.test("Property 1: Invite creates auth user and matching profile", async () => {
  await fc.assert(
    fc.asyncProperty(validInvitePayload, async (payload) => {
      const authUserId = crypto.randomUUID();
      const profileId = crypto.randomUUID();

      const { client, calls } = createMockSupabaseAdmin({
        createUserResult: {
          data: { user: { id: authUserId } },
          error: null,
        },
        insertResult: {
          data: { id: profileId },
          error: null,
        },
        generateLinkResult: {
          data: { properties: { action_link: "https://example.com/link" } },
          error: null,
        },
      });

      // Stub global fetch for Resend API call
      const originalFetch = globalThis.fetch;
      globalThis.fetch = ((_input: any, _init?: any) =>
        Promise.resolve(new Response(JSON.stringify({ id: "email-id" }), { status: 200 }))) as any;

      try {
        const response = await handleInvite(client, "test-resend-key", {
          name: payload.name,
          email: payload.email,
          role: payload.role,
          workerId: payload.workerId,
        });

        const json = await response.json();

        // Auth user was created with the correct email
        assertEquals(calls.createUser.length, 1);
        assertEquals(calls.createUser[0].email, payload.email);
        assertEquals(calls.createUser[0].email_confirm, true);
        assertEquals(calls.createUser[0].user_metadata.name, payload.name);

        // Profile was inserted with matching fields
        assertEquals(calls.insert.length, 1);
        const insertedRow = calls.insert[0];
        assertEquals(insertedRow.auth_user_id, authUserId);
        assertEquals(insertedRow.name, payload.name);
        assertEquals(insertedRow.email, payload.email);
        assertEquals(insertedRow.role, payload.role);
        assertEquals(insertedRow.is_active, true);
        if (payload.workerId) {
          assertEquals(insertedRow.worker_id, payload.workerId);
        }

        // Response indicates success
        assertEquals(json.success, true);
        assertEquals(json.userId, authUserId);
        assertEquals(json.profileId, profileId);
      } finally {
        globalThis.fetch = originalFetch;
      }
    }),
    { numRuns: 100 }
  );
});


// ── Property 2: Auth failure prevents profile creation ──────────────────────
// Feature: user-invite-flow, Property 2: Auth failure prevents profile creation
// **Validates: Requirements 1.4**

Deno.test("Property 2: Auth failure prevents profile creation", async () => {
  await fc.assert(
    fc.asyncProperty(validInvitePayload, async (payload) => {
      const { client, calls } = createMockSupabaseAdmin({
        createUserResult: {
          data: { user: null },
          error: { message: "Auth creation failed", status: 500 },
        },
      });

      const response = await handleInvite(client, "test-resend-key", {
        name: payload.name,
        email: payload.email,
        role: payload.role,
        workerId: payload.workerId,
      });

      const json = await response.json();

      // Response is an error
      assertEquals(json.success, false);
      assertEquals(response.status, 500);

      // No profile insert was attempted
      assertEquals(calls.insert.length, 0);

      // createUser was called (it just failed)
      assertEquals(calls.createUser.length, 1);
    }),
    { numRuns: 100 }
  );
});


// ── Property 3: Profile failure triggers auth rollback ──────────────────────
// Feature: user-invite-flow, Property 3: Profile failure triggers auth rollback
// **Validates: Requirements 1.5**

Deno.test("Property 3: Profile failure triggers auth rollback", async () => {
  await fc.assert(
    fc.asyncProperty(validInvitePayload, async (payload) => {
      const authUserId = crypto.randomUUID();

      const { client, calls } = createMockSupabaseAdmin({
        createUserResult: {
          data: { user: { id: authUserId } },
          error: null,
        },
        insertResult: {
          data: null,
          error: { message: "Insert failed", code: "23505" },
        },
      });

      const response = await handleInvite(client, "test-resend-key", {
        name: payload.name,
        email: payload.email,
        role: payload.role,
        workerId: payload.workerId,
      });

      const json = await response.json();

      // Response is an error
      assertEquals(json.success, false);
      assertEquals(response.status, 500);
      assertStringIncludes(json.error, "Profile creation failed");

      // Auth user was created
      assertEquals(calls.createUser.length, 1);

      // Profile insert was attempted
      assertEquals(calls.insert.length, 1);

      // Auth user was deleted (rollback)
      assertEquals(calls.deleteUser.length, 1);
      assertEquals(calls.deleteUser[0], authUserId);
    }),
    { numRuns: 100 }
  );
});


// ── Property 4: Successful invite generates recovery link and sends email ───
// Feature: user-invite-flow, Property 4: Successful invite generates recovery link and sends email
// **Validates: Requirements 2.1, 2.2**

Deno.test("Property 4: Successful invite generates recovery link and sends email", async () => {
  await fc.assert(
    fc.asyncProperty(validInvitePayload, async (payload) => {
      const authUserId = crypto.randomUUID();
      const profileId = crypto.randomUUID();
      const recoveryLink = "https://example.com/recovery-token-abc";

      const { client, calls } = createMockSupabaseAdmin({
        createUserResult: {
          data: { user: { id: authUserId } },
          error: null,
        },
        insertResult: {
          data: { id: profileId },
          error: null,
        },
        generateLinkResult: {
          data: { properties: { action_link: recoveryLink } },
          error: null,
        },
      });

      // Capture Resend API calls
      const resendCalls: { url: string; body: any }[] = [];
      const originalFetch = globalThis.fetch;
      globalThis.fetch = ((input: any, init?: any) => {
        resendCalls.push({
          url: typeof input === "string" ? input : input.url,
          body: JSON.parse(init?.body || "{}"),
        });
        return Promise.resolve(
          new Response(JSON.stringify({ id: "email-id" }), { status: 200 })
        );
      }) as any;

      try {
        const response = await handleInvite(client, "test-resend-key", {
          name: payload.name,
          email: payload.email,
          role: payload.role,
          workerId: payload.workerId,
        });

        const json = await response.json();

        // generateLink was called with type "recovery" and correct redirect URL
        assertEquals(calls.generateLink.length, 1);
        assertEquals(calls.generateLink[0].type, "recovery");
        assertEquals(calls.generateLink[0].email, payload.email);
        assertEquals(
          calls.generateLink[0].options.redirectTo,
          "https://jedmcdaniel253.github.io/AuntiesCleaners/set-password"
        );

        // Resend API was called with the user's email
        assertEquals(resendCalls.length, 1);
        assertEquals(resendCalls[0].url, "https://api.resend.com/emails");
        assertEquals(resendCalls[0].body.to[0], payload.email);

        // Response indicates success with email sent
        assertEquals(json.success, true);
        assertEquals(json.emailSent, true);
      } finally {
        globalThis.fetch = originalFetch;
      }
    }),
    { numRuns: 100 }
  );
});


// ── Property 5: Invite email contains required content ──────────────────────
// Feature: user-invite-flow, Property 5: Invite email contains required content
// **Validates: Requirements 2.3**

Deno.test("Property 5: Invite email contains required content", async () => {
  await fc.assert(
    fc.property(nonEmptyName, validUrl, (name, recoveryUrl) => {
      const html = buildInviteEmailHtml(name, recoveryUrl);

      // Contains the user's name
      assertStringIncludes(html, name);

      // Contains a welcome message
      assertStringIncludes(html, "Welcome to Auntie's Cleaners");

      // Contains the recovery URL (as a clickable link)
      assertStringIncludes(html, recoveryUrl);

      // Contains an anchor tag with the recovery URL (clickable link)
      assertStringIncludes(html, `href="${recoveryUrl}"`);
    }),
    { numRuns: 100 }
  );
});
