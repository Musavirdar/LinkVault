/**
 * Fake OpenAI-Compatible Test Double Server for Meta Muse Spark Open
 *
 * Used exclusively for Playwright end-to-end testing without calling
 * live external Baseten endpoints or using real API credentials.
 */

import http from 'http';

let simulateError = false;

export function setSimulateError(value: boolean) {
  simulateError = value;
}

export function createFakeModelServer(port = 5055): http.Server {
  const server = http.createServer((req, res) => {
    // CORS headers
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

    if (req.method === 'OPTIONS') {
      res.writeHead(204);
      res.end();
      return;
    }

    if (req.url === '/health' && req.method === 'GET') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'ok', model: 'fake-meta-models/muse-spark-open-test-double' }));
      return;
    }

    if (req.url === '/__control/fail' && req.method === 'POST') {
      simulateError = true;
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ simulateError: true }));
      return;
    }

    if (req.url === '/__control/reset' && req.method === 'POST') {
      simulateError = false;
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ simulateError: false }));
      return;
    }

    if (req.url?.includes('/chat/completions') && req.method === 'POST') {
      let body = '';
      req.on('data', chunk => {
        body += chunk;
      });

      req.on('end', () => {
        if (simulateError) {
          res.writeHead(500, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: 'Simulated Meta model server 500 outage' }));
          return;
        }

        try {
          const parsed = JSON.parse(body || '{}');
          const messages: Array<{ role: string; content: string }> = parsed.messages || [];
          const allText = messages.map(m => m.content || '').join('\n');
          const lastUserMessage = [...messages].reverse().find(m => m.role === 'user')?.content || '';

          // 1. Check for failure simulation via prompt
          if (lastUserMessage.toLowerCase().includes('simulate-model-failure') || lastUserMessage.toLowerCase().includes('crash model')) {
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Simulated Meta model server 500 outage' }));
            return;
          }

          // 2. Prompt: "Create a folder called Q3 Reports"
          if (lastUserMessage.toLowerCase().includes('q3 reports')) {
            const response = {
              id: 'chatcmpl-test-q3',
              object: 'chat.completion',
              created: Math.floor(Date.now() / 1000),
              model: 'meta-models/muse-spark-open',
              choices: [
                {
                  index: 0,
                  message: {
                    role: 'assistant',
                    content: null,
                    tool_calls: [
                      {
                        id: 'call_create_q3',
                        type: 'function',
                        function: {
                          name: 'create_directory',
                          arguments: JSON.stringify({ name: 'Q3 Reports' })
                        }
                      }
                    ]
                  },
                  finish_reason: 'tool_calls'
                }
              ]
            };
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(response));
            return;
          }

          // 3. Prompt: Reject Flow (e.g. "Create a folder called Unwanted Folder")
          if (lastUserMessage.toLowerCase().includes('unwanted folder') || lastUserMessage.toLowerCase().includes('reject')) {
            const response = {
              id: 'chatcmpl-test-reject',
              object: 'chat.completion',
              created: Math.floor(Date.now() / 1000),
              model: 'meta-models/muse-spark-open',
              choices: [
                {
                  index: 0,
                  message: {
                    role: 'assistant',
                    content: null,
                    tool_calls: [
                      {
                        id: 'call_create_unwanted',
                        type: 'function',
                        function: {
                          name: 'create_directory',
                          arguments: JSON.stringify({ name: 'Unwanted Folder' })
                        }
                      }
                    ]
                  },
                  finish_reason: 'tool_calls'
                }
              ]
            };
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(response));
            return;
          }

          // 4. Prompt: Safe Share - Share seeded fake credentials file
          if (lastUserMessage.toLowerCase().includes('database_backup_config') || lastUserMessage.toLowerCase().includes('credentials') || (lastUserMessage.toLowerCase().includes('share') && lastUserMessage.toLowerCase().includes('fake'))) {
            // Find resource ID of database_backup_config.env from context
            let targetResourceId = '00000000-0000-0000-0000-000000000001';
            const idMatch = allText.match(/"id":\s*"([0-9a-fA-F-]{36})"[^}]*database_backup_config\.env/i)
              || allText.match(/database_backup_config\.env[^}]*"id":\s*"([0-9a-fA-F-]{36})"/i);
            if (idMatch) {
              targetResourceId = idMatch[1];
            } else {
              // Try finding any resource id in recent resources
              const anyIdMatch = allText.match(/"id":\s*"([0-9a-fA-F-]{36})"/);
              if (anyIdMatch) targetResourceId = anyIdMatch[1];
            }

            const response = {
              id: 'chatcmpl-test-safe-share',
              object: 'chat.completion',
              created: Math.floor(Date.now() / 1000),
              model: 'meta-models/muse-spark-open',
              choices: [
                {
                  index: 0,
                  message: {
                    role: 'assistant',
                    content: null,
                    tool_calls: [
                      {
                        id: 'call_share_credentials',
                        type: 'function',
                        function: {
                          name: 'create_share_link',
                          arguments: JSON.stringify({ resourceId: targetResourceId, expiryDays: 7 })
                        }
                      }
                    ]
                  },
                  finish_reason: 'tool_calls'
                }
              ]
            };
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(response));
            return;
          }

          // 5. Prompt: Generic Share (e.g. for Member role test)
          if (lastUserMessage.toLowerCase().includes('share')) {
            let targetResourceId = '00000000-0000-0000-0000-000000000002';
            const anyIdMatch = allText.match(/"id":\s*"([0-9a-fA-F-]{36})"/);
            if (anyIdMatch) targetResourceId = anyIdMatch[1];

            const response = {
              id: 'chatcmpl-test-member-share',
              object: 'chat.completion',
              created: Math.floor(Date.now() / 1000),
              model: 'meta-models/muse-spark-open',
              choices: [
                {
                  index: 0,
                  message: {
                    role: 'assistant',
                    content: null,
                    tool_calls: [
                      {
                        id: 'call_member_share',
                        type: 'function',
                        function: {
                          name: 'create_share_link',
                          arguments: JSON.stringify({ resourceId: targetResourceId, expiryDays: 3 })
                        }
                      }
                    ]
                  },
                  finish_reason: 'tool_calls'
                }
              ]
            };
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(response));
            return;
          }

          // Default fallback: conversational message without tool calls
          const defaultResponse = {
            id: 'chatcmpl-test-default',
            object: 'chat.completion',
            created: Math.floor(Date.now() / 1000),
            model: 'meta-models/muse-spark-open',
            choices: [
              {
                index: 0,
                message: {
                  role: 'assistant',
                  content: 'Test double model responded without tool calls.'
                },
                finish_reason: 'stop'
              }
            ]
          };
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify(defaultResponse));
        } catch (e: any) {
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: e.message }));
        }
      });
      return;
    }

    // Unhandled endpoint
    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'Endpoint not found on fake model test double' }));
  });

  return server;
}

// Allow running directly as standalone CLI process
const isMain = Boolean(process.argv[1] && (
  process.argv[1].endsWith('fake-model.ts') || 
  process.argv[1].endsWith('fake-model.js') || 
  process.argv[1].includes('fake-model')
));

if (isMain) {
  const port = parseInt(process.env.FAKE_MODEL_PORT || '5055', 10);
  const server = createFakeModelServer(port);
  server.listen(port, () => {
    console.log(`[FakeModelServer] Test double listening on http://localhost:${port}`);
  });
}
