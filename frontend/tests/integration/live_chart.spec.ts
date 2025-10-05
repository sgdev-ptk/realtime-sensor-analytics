import { test, expect } from '@playwright/test';

test.describe('Live Chart', () => {
  test('updates smoothly at ~30 FPS (placeholder)', async ({ page }) => {
    await page.goto('http://localhost:4200');
    // TODO: Wait for chart and measure frame updates
    await expect(page).toHaveTitle(/frontend/i);
  });
});
