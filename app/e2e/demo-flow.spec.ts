import { test, expect } from '@playwright/test';
import type { Server } from 'http';
import { createFakeModelServer } from './fake-model';

let fakeServer: Server | null = null;

test.beforeAll(async () => {
  try {
    fakeServer = createFakeModelServer(5055);
    await new Promise<void>((resolve, reject) => {
      fakeServer!.listen(5055, () => {
        console.log('[E2E Test Hook] In-process fake model test double listening on http://localhost:5055');
        resolve();
      });
      fakeServer!.on('error', (err: any) => {
        if (err.code === 'EADDRINUSE') {
          console.log('[E2E Test Hook] Port 5055 already in use (external fake-model running), reusing.');
          fakeServer = null;
          resolve();
        } else {
          reject(err);
        }
      });
    });
  } catch (e) {
    console.warn('[E2E Test Hook] Fake model server start notice:', e);
  }
});

test.afterAll(async () => {
  if (fakeServer) {
    await new Promise<void>((resolve) => fakeServer!.close(() => resolve()));
  }
});

test.describe.configure({ mode: 'serial' });

test.describe('LinkVault Demo Flow & Agent End-to-End Tests', () => {

  test('1. Login page "Try Demo (Admin)" logs in and lands on the dashboard', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByTestId('demo-admin-btn')).toBeVisible({ timeout: 10000 });
    
    // Click Demo Admin button
    await page.getByTestId('demo-admin-btn').click();
    
    // Verify redirection to dashboard
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 });
    
    // Check for dashboard content
    const heading = page.locator('h1, h2').or(page.getByText('Workspace', { exact: false })).first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('2. Demo Member cannot see audit logs and cannot approve a share plan', async ({ page }) => {
    // 2a. Log in as Demo Member
    await page.goto('/login');
    await expect(page.getByTestId('demo-member-btn')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('demo-member-btn').click();
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 });

    // 2b. Demo Member navigates to /audit - should see no audit logs or permission denial
    await page.goto('/audit');
    const denialOrEmpty = page.getByText(/permission|denied|No audit events found|0 events recorded/i);
    await expect(denialOrEmpty.first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('via-agent-badge')).toHaveCount(0);

    // 2c. Demo Member tries to approve a share plan on /agent
    await page.goto('/agent');
    await expect(page.getByTestId('agent-prompt-input')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('agent-prompt-input').fill('Share document with team');
    await page.getByTestId('agent-submit-btn').click();

    // Plan preview modal appears awaiting approval
    await expect(page.getByTestId('plan-preview-modal')).toBeVisible({ timeout: 15000 });
    await page.getByTestId('approve-plan-btn').click();

    // Verify denial: Member lacks canShare claim so execution fails
    const permissionError = page.locator('[data-testid="live-progress-stream"]').getByText(/permission|denied|canShare|Failed/i)
      .or(page.getByText(/permission denied|lacks 'canShare'/i))
      .or(page.getByTestId('agent-error-banner'));
    await expect(permissionError.first()).toBeVisible({ timeout: 15000 });
  });

  test('3. Agent page: request "Create a folder called Q3 Reports" -> approve -> folder in Directories', async ({ page }) => {
    // Log back in as Demo Admin
    await page.goto('/login');
    await page.getByTestId('demo-admin-btn').click();
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 });

    // Go to Agent page
    await page.goto('/agent');
    await expect(page.getByTestId('agent-prompt-input')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('agent-prompt-input').fill('Create a folder called Q3 Reports');
    await page.getByTestId('agent-submit-btn').click();

    // Verify plan preview modal appears with step
    await expect(page.getByTestId('plan-preview-modal')).toBeVisible({ timeout: 15000 });
    await expect(page.getByText('Q3 Reports').or(page.getByText('create_directory'))).toBeVisible();

    // Click Approve
    await page.getByTestId('approve-plan-btn').click();

    // Verify step turns completed in LiveProgressStream
    await expect(page.getByTestId('live-progress-stream')).toBeVisible({ timeout: 10000 });
    const completedBadge = page.locator('[data-testid="live-progress-stream"]').getByText(/Completed|Execution Done|Done/i).first();
    await expect(completedBadge).toBeVisible({ timeout: 15000 });

    // Navigate to Directories and verify "Q3 Reports" is present
    await page.goto('/directories');
    await expect(page.getByText('Q3 Reports').first()).toBeVisible({ timeout: 10000 });
  });

  test('4. Reject flow: request a plan, click Reject -> no changes in workspace', async ({ page }) => {
    await page.goto('/agent');
    await expect(page.getByTestId('agent-prompt-input')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('agent-prompt-input').fill('Create a folder called Unwanted Folder');
    await page.getByTestId('agent-submit-btn').click();

    // Plan preview modal appears
    await expect(page.getByTestId('plan-preview-modal')).toBeVisible({ timeout: 15000 });

    // Click Reject
    await page.getByTestId('reject-plan-btn').click();

    // Modal closes
    await expect(page.getByTestId('plan-preview-modal')).not.toBeVisible({ timeout: 5000 });

    // Navigate to Directories and confirm "Unwanted Folder" does NOT exist
    await page.goto('/directories');
    await expect(page.getByText('Unwanted Folder')).toHaveCount(0);
  });

  test('5. Safe Share: ask agent to share fake credentials file -> step blocked and warning shown', async ({ page }) => {
    await page.goto('/agent');
    await expect(page.getByTestId('agent-prompt-input')).toBeVisible({ timeout: 10000 });
    await page.getByTestId('agent-prompt-input').fill('Share database_backup_config.env with team');
    await page.getByTestId('agent-submit-btn').click();

    // Plan modal appears awaiting approval
    await expect(page.getByTestId('plan-preview-modal')).toBeVisible({ timeout: 15000 });
    await page.getByTestId('approve-plan-btn').click();

    // Step execution triggers Safe Share Guard which blocks the step and shows warning
    const safeShareWarning = page.getByTestId('safe-share-alert-modal')
      .or(page.locator('[data-testid="live-progress-stream"]').getByText(/Safe Share Guard blocked|Execution Halted|Failed/i))
      .or(page.getByText(/Safe Share Guard: Share Blocked|Exposed Database Credentials|blocked/i));

    await expect(safeShareWarning.first()).toBeVisible({ timeout: 15000 });
  });

  test('6. Audit page shows entries with the "via agent" badge after test 3', async ({ page }) => {
    await page.goto('/audit');
    // Ensure audit logs are loaded and the "via agent" badge from directory creation is rendered
    await expect(page.getByTestId('via-agent-badge').first()).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId('via-agent-badge').first()).toContainText('via agent');
  });

  test('7. Model unavailable: when model server returns 500, agent page shows 503 error and no plan is created', async ({ page }) => {
    await page.goto('/agent');
    await expect(page.getByTestId('agent-prompt-input')).toBeVisible({ timeout: 10000 });
    
    // Prompt triggering simulated 500 response from fake model
    await page.getByTestId('agent-prompt-input').fill('simulate-model-failure: please organize files');
    await page.getByTestId('agent-submit-btn').click();

    // Verify 503 error message banner is shown
    await expect(page.getByTestId('agent-error-banner')).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId('agent-error-banner')).toContainText(/503|unavailable|outage|failed/i);

    // Verify no plan preview modal is created
    await expect(page.getByTestId('plan-preview-modal')).not.toBeVisible();
  });
});
