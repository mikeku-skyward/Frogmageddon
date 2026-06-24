let animationFrameId = null;
let isContextLost = false;
let backgroundImage = null;
let backgroundLoaded = false;

// Preload the background image
(function loadBackground() {
    backgroundImage = new Image();
    backgroundImage.onload = () => { backgroundLoaded = true; };
    backgroundImage.onerror = () => { console.error('Failed to load background image.'); };
    backgroundImage.src = './images/game background.jpg';
})();

/**
 * Initializes the game loop: gets the 2D canvas context, registers keyboard
 * listeners, and starts the requestAnimationFrame loop.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element (passed via Blazor ElementReference)
 * @param {object} dotNetRef - DotNetObjectReference for invoking C# methods
 */
export function initializeGame(canvasElement, dotNetRef) {
    try {
        const ctx = canvasElement.getContext('2d');
        if (!ctx) {
            throw new Error('Failed to acquire 2D canvas context.');
        }

        let lastTimestamp = 0;

        // Handle canvas context loss: pause the rendering loop
        canvasElement.addEventListener('contextlost', (e) => {
            e.preventDefault();
            isContextLost = true;
        });

        // Handle canvas context restored: re-acquire context and resume
        canvasElement.addEventListener('contextrestored', () => {
            isContextLost = false;
            canvasElement.getContext('2d');
            if (animationFrameId === null) {
                lastTimestamp = 0;
                animationFrameId = requestAnimationFrame(gameLoop);
            }
        });

        // Keyboard handling – convert key to lowercase before sending to C#
        document.addEventListener('keydown', (e) => {
            dotNetRef.invokeMethodAsync('SetKeyDown', e.key.toLowerCase());
        });

        document.addEventListener('keyup', (e) => {
            dotNetRef.invokeMethodAsync('SetKeyUp', e.key.toLowerCase());
        });

        // Mouse click handling – compute coordinates relative to canvas
        canvasElement.addEventListener('click', (e) => {
            const rect = canvasElement.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            dotNetRef.invokeMethodAsync('OnMouseClick', x, y);
        });

        // Mouse move handling – track cursor position relative to canvas
        canvasElement.addEventListener('mousemove', (e) => {
            const rect = canvasElement.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            dotNetRef.invokeMethodAsync('OnMouseMove', x, y);
        });

        // Game loop via requestAnimationFrame
        function gameLoop(timestamp) {
            if (isContextLost) {
                animationFrameId = null;
                return;
            }

            if (lastTimestamp === 0) {
                lastTimestamp = timestamp;
            }

            const deltaTime = timestamp - lastTimestamp;
            lastTimestamp = timestamp;

            dotNetRef.invokeMethodAsync('Tick', deltaTime);

            animationFrameId = requestAnimationFrame(gameLoop);
        }

        animationFrameId = requestAnimationFrame(gameLoop);
    } catch (error) {
        console.error('Game initialization failed:', error);
        throw error;
    }
}

/**
 * Clears the full canvas.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 */
export function clearCanvas(canvasElement) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);
}

/**
 * Renders a full frame: background with camera offset, then player and frogs.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} cameraX - Camera top-left X in world space
 * @param {number} cameraY - Camera top-left Y in world space
 * @param {number} playerX - Player X position in world space
 * @param {number} playerY - Player Y position in world space
 * @param {number} rotation - Player rotation in radians
 * @param {number} size - Player size for triangle dimensions
 * @param {number[]} frogData - Flat array of frog data [x, y, rotation, size, ...]
 * @param {number[]} bulletData - Flat array of bullet data [x, y, radius, ...]
 * @param {boolean} isFlashing - Whether the player is flashing (damage)
 * @param {number} currentAmmo - Current ammo count
 * @param {number} maxAmmo - Maximum ammo capacity
 * @param {boolean} isReloading - Whether reload is in progress
 * @param {number} reloadProgress - Reload progress ratio (0-1)
 * @param {number} playerScreenX - Player screen X position
 * @param {number} playerScreenY - Player screen Y position
 * @param {number} playerSize - Player radius for positioning the reload bar
 * @param {number} staminaRatio - Current stamina ratio (0.0 to 1.0) for the HUD bar
 */
export function renderFrame(canvasElement, cameraX, cameraY, playerX, playerY, rotation, size, frogData, bulletData, isFlashing, currentAmmo, maxAmmo, isReloading, reloadProgress, playerScreenX, playerScreenY, playerSize, staminaRatio) {
    const ctx = canvasElement.getContext('2d');
    const viewWidth = canvasElement.width;
    const viewHeight = canvasElement.height;

    // Clear the canvas
    ctx.clearRect(0, 0, viewWidth, viewHeight);

    // Draw the background image, offset by camera position
    if (backgroundLoaded && backgroundImage) {
        const imgW = backgroundImage.naturalWidth;
        const imgH = backgroundImage.naturalHeight;

        const worldWidth = 2000;
        const worldHeight = 1500;

        // Source coordinates in the image
        const sx = (cameraX / worldWidth) * imgW;
        const sy = (cameraY / worldHeight) * imgH;
        const sw = (viewWidth / worldWidth) * imgW;
        const sh = (viewHeight / worldHeight) * imgH;

        ctx.drawImage(backgroundImage, sx, sy, sw, sh, 0, 0, viewWidth, viewHeight);
    } else {
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, viewWidth, viewHeight);
    }

    // Draw frogs
    if (frogData && frogData.length > 0) {
        for (let i = 0; i < frogData.length; i += 4) {
            const frogX = frogData[i] - cameraX;
            const frogY = frogData[i + 1] - cameraY;
            const frogRotation = frogData[i + 2];
            const frogSize = frogData[i + 3];

            // Only draw if on screen (with margin)
            if (frogX < -frogSize * 2 || frogX > viewWidth + frogSize * 2 ||
                frogY < -frogSize * 2 || frogY > viewHeight + frogSize * 2) {
                continue;
            }

            drawFrog(ctx, frogX, frogY, frogRotation, frogSize);
        }
    }

    // Draw bullets
    if (bulletData && bulletData.length > 0) {
        ctx.fillStyle = '#ffff00';
        for (let i = 0; i < bulletData.length; i += 3) {
            const bx = bulletData[i] - cameraX;
            const by = bulletData[i + 1] - cameraY;
            const br = bulletData[i + 2];

            // Only draw if on screen
            if (bx < -br || bx > viewWidth + br || by < -br || by > viewHeight + br) {
                continue;
            }

            ctx.beginPath();
            ctx.arc(bx, by, br, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    // Draw the player
    const screenX = playerX - cameraX;
    const screenY = playerY - cameraY;

    ctx.save();
    ctx.translate(screenX, screenY);
    ctx.rotate(rotation);

    ctx.beginPath();
    ctx.moveTo(size, 0);
    ctx.lineTo(-size * 0.7, -size * 0.6);
    ctx.lineTo(-size * 0.7, size * 0.6);
    ctx.closePath();

    ctx.fillStyle = isFlashing ? 'red' : 'cyan';
    ctx.fill();

    ctx.restore();

    // Draw ammo HUD in bottom-right corner
    ctx.save();
    ctx.font = 'bold 18px monospace';
    ctx.fillStyle = '#ffffff';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'bottom';
    ctx.fillText(currentAmmo + '/' + maxAmmo, viewWidth - 32, viewHeight - 32);
    ctx.restore();

    // Draw reload progress bar above the player when reloading
    if (isReloading) {
        const barWidth = 40;
        const barHeight = 6;
        const barX = playerScreenX - barWidth / 2;
        const barY = playerScreenY - playerSize - 20;

        // Background (unfilled area)
        ctx.fillStyle = '#333333';
        ctx.fillRect(barX, barY, barWidth, barHeight);

        // Filled portion (progress)
        ctx.fillStyle = '#00ff00';
        ctx.fillRect(barX, barY, barWidth * reloadProgress, barHeight);
    }

    // Draw stamina bar in top-left HUD area, below health text overlay
    const staminaBarX = 10;
    const staminaBarY = 34;
    const staminaBarWidth = 200;
    const staminaBarHeight = 12;

    // Background (dark gray)
    ctx.fillStyle = '#333333';
    ctx.fillRect(staminaBarX, staminaBarY, staminaBarWidth, staminaBarHeight);

    // Fill (green) scaled by staminaRatio from the left edge
    ctx.fillStyle = '#00cc00';
    ctx.fillRect(staminaBarX, staminaBarY, staminaBarWidth * staminaRatio, staminaBarHeight);
}

/**
 * Draws a frog as a green blob with eyes, facing its rotation direction.
 */
function drawFrog(ctx, x, y, rotation, size) {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(rotation);

    // Body - oval shape
    ctx.beginPath();
    ctx.ellipse(0, 0, size, size * 0.7, 0, 0, Math.PI * 2);
    ctx.fillStyle = '#2d8a2d';
    ctx.fill();
    ctx.strokeStyle = '#1a5c1a';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Eyes - two circles at the front
    const eyeOffset = size * 0.5;
    const eyeSpread = size * 0.4;
    const eyeRadius = size * 0.25;

    // Left eye
    ctx.beginPath();
    ctx.arc(eyeOffset, -eyeSpread, eyeRadius, 0, Math.PI * 2);
    ctx.fillStyle = '#ffffcc';
    ctx.fill();
    ctx.beginPath();
    ctx.arc(eyeOffset + eyeRadius * 0.3, -eyeSpread, eyeRadius * 0.5, 0, Math.PI * 2);
    ctx.fillStyle = '#111';
    ctx.fill();

    // Right eye
    ctx.beginPath();
    ctx.arc(eyeOffset, eyeSpread, eyeRadius, 0, Math.PI * 2);
    ctx.fillStyle = '#ffffcc';
    ctx.fill();
    ctx.beginPath();
    ctx.arc(eyeOffset + eyeRadius * 0.3, eyeSpread, eyeRadius * 0.5, 0, Math.PI * 2);
    ctx.fillStyle = '#111';
    ctx.fill();

    ctx.restore();
}

/**
 * Draws the player as a triangle ship shape at the given position and rotation.
 * Kept for backward compatibility.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} x - X position
 * @param {number} y - Y position
 * @param {number} rotation - Rotation in radians
 * @param {number} size - Size used for triangle dimensions
 */
export function drawPlayer(canvasElement, x, y, rotation, size) {
    const ctx = canvasElement.getContext('2d');

    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(rotation);

    ctx.beginPath();
    ctx.moveTo(size, 0);
    ctx.lineTo(-size * 0.7, -size * 0.6);
    ctx.lineTo(-size * 0.7, size * 0.6);
    ctx.closePath();

    ctx.fillStyle = 'cyan';
    ctx.fill();

    ctx.restore();
}

/**
 * Draws the start screen with title and start button.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} canvasWidth - Canvas width
 * @param {number} canvasHeight - Canvas height
 * @param {number} btnX - Button X position
 * @param {number} btnY - Button Y position
 * @param {number} btnW - Button width
 * @param {number} btnH - Button height
 */
export function drawStartScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Title text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Frogmageddon', canvasWidth / 2, canvasHeight / 2 - 60);

    // Start button rectangle
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, btnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, btnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Start', btnX + btnW / 2, btnY + btnH / 2);
}

/**
 * Draws the game over screen.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} canvasWidth - Canvas width
 * @param {number} canvasHeight - Canvas height
 * @param {number} btnX - Button X position
 * @param {number} btnY - Button Y position
 * @param {number} btnW - Button width
 * @param {number} btnH - Button height
 */
export function drawGameOverScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Dark background
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, canvasWidth, canvasHeight);

    // Game over text in red
    ctx.fillStyle = 'red';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('You died, Game Over.', canvasWidth / 2, canvasHeight / 2 - 60);

    // Restart button rectangle
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, btnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, btnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Restart', btnX + btnW / 2, btnY + btnH / 2);
}
