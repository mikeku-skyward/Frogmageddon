let animationFrameId = null;
let isContextLost = false;
let backgroundImage = null;
let backgroundLoaded = false;
let _cachedRect = null;
let _offscreenCanvas = null;
let _offscreenCtx = null;

// Preload the background image
(function loadBackground() {
    backgroundImage = new Image();
    backgroundImage.onload = () => { backgroundLoaded = true; };
    backgroundImage.onerror = () => { console.error('Failed to load background image.'); };
    backgroundImage.src = './images/game background.jpg';
})();

// Player sprite preloading
let playerSprites = [null, null, null];
let playerSpritesLoaded = false;

(function loadPlayerSprites() {
    const spritePaths = [
        './images/stationary.png',   // index 0 - stationary
        './images/walk 1.png',       // index 1 - walk1
        './images/walk 2.png'        // index 2 - walk2
    ];

    let loadedCount = 0;
    let failed = false;

    spritePaths.forEach((path, index) => {
        const img = new Image();
        let timedOut = false;

        const timeout = setTimeout(() => {
            timedOut = true;
            failed = true;
            console.error('Timeout loading player sprite: ' + path);
        }, 10000);

        img.onload = () => {
            if (timedOut) return;
            clearTimeout(timeout);
            playerSprites[index] = img;
            loadedCount++;
            if (loadedCount === 3 && !failed) {
                playerSpritesLoaded = true;
            }
        };

        img.onerror = () => {
            if (timedOut) return;
            clearTimeout(timeout);
            failed = true;
            console.error('Failed to load player sprite: ' + path);
        };

        img.src = path;
    });
})();

// Frog sprite preloading
let frogSprites = [null, null]; // index 0 = stationary, index 1 = jumping
let frogSpritesLoaded = false;

(function loadFrogSprites() {
    const spritePaths = [
        './images/frog stationary.png',  // index 0 - sitting
        './images/frog jump.png'         // index 1 - hopping
    ];

    let loadedCount = 0;
    let failed = false;

    spritePaths.forEach((path, index) => {
        const img = new Image();
        let timedOut = false;

        const timeout = setTimeout(() => {
            timedOut = true;
            failed = true;
            console.error('Timeout loading frog sprite: ' + path);
        }, 10000);

        img.onload = () => {
            if (timedOut) return;
            clearTimeout(timeout);
            frogSprites[index] = img;
            loadedCount++;
            if (loadedCount === 2 && !failed) {
                frogSpritesLoaded = true;
            }
        };

        img.onerror = () => {
            if (timedOut) return;
            clearTimeout(timeout);
            failed = true;
            console.error('Failed to load frog sprite: ' + path);
        };

        img.src = path;
    });
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

        // Create cached offscreen canvas for damage flash compositing
        _offscreenCanvas = document.createElement('canvas');
        _offscreenCanvas.width = 128;
        _offscreenCanvas.height = 128;
        _offscreenCtx = _offscreenCanvas.getContext('2d');

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

        // Cache bounding rect for mouse coordinate calculations
        _cachedRect = canvasElement.getBoundingClientRect();

        // Invalidate and recompute on window resize
        window.addEventListener('resize', () => {
            _cachedRect = canvasElement.getBoundingClientRect();
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
            const rect = _cachedRect || canvasElement.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            dotNetRef.invokeMethodAsync('OnMouseClick', x, y);
        });

        // Mouse move handling – track cursor position relative to canvas
        canvasElement.addEventListener('mousemove', (e) => {
            const rect = _cachedRect || canvasElement.getBoundingClientRect();
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
 * @param {number} animationFrameIndex - Current sprite index (0=stationary, 1=walk1, 2=walk2)
 * @param {number} facingDirection - Facing direction (0=right, 1=left)
 * @param {number|null} frogCount - Number of active frogs (used to limit frogData reading); null means use full array
 * @param {number|null} bulletCount - Number of active bullets (used to limit bulletData reading); null means use full array
 */
export function renderFrame(canvasElement, cameraX, cameraY, playerX, playerY, rotation, size, frogData, bulletData, isFlashing, currentAmmo, maxAmmo, isReloading, reloadProgress, playerScreenX, playerScreenY, playerSize, staminaRatio, animationFrameIndex, facingDirection, frogCount, bulletCount, healthRatio) {
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
    const frogDataLength = frogCount != null ? frogCount * 5 : (frogData ? frogData.length : 0);
    if (frogData && frogDataLength > 0) {
        for (let i = 0; i < frogDataLength; i += 5) {
            const frogX = frogData[i] - cameraX;
            const frogY = frogData[i + 1] - cameraY;
            const frogRotation = frogData[i + 2];
            const frogSize = frogData[i + 3];
            const isHopping = frogData[i + 4] > 0.5;

            // Only draw if on screen (with margin)
            if (frogX < -frogSize * 2 || frogX > viewWidth + frogSize * 2 ||
                frogY < -frogSize * 2 || frogY > viewHeight + frogSize * 2) {
                continue;
            }

            drawFrog(ctx, frogX, frogY, frogRotation, frogSize, isHopping);
        }
    }

    // Draw the player (after frogs, before bullets)
    if (playerSpritesLoaded) {
        drawPlayerSprite(ctx, playerScreenX, playerScreenY, playerSize, animationFrameIndex, facingDirection === 1, isFlashing);
    } else {
        // Fallback: draw the triangle
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
    }

    // Draw bullets
    const bulletDataLength = bulletCount != null ? bulletCount * 3 : (bulletData ? bulletData.length : 0);
    if (bulletData && bulletDataLength > 0) {
        ctx.fillStyle = '#ffff00';
        for (let i = 0; i < bulletDataLength; i += 3) {
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

    // Draw health bar in top-left
    const healthBarWidth = 130;
    const healthBarHeight = 18;
    const staminaBarHeight = 8;
    const iconSize = 18;
    const iconX = 10;
    const barStartX = iconX + iconSize + 6;
    const healthBarY = 10;

    // Heart icon ♥
    ctx.font = `${iconSize}px monospace`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    const iconCenterX = iconX + iconSize / 2;
    ctx.fillStyle = '#ff4444';
    ctx.fillText('♥', iconCenterX, healthBarY + healthBarHeight / 2);

    // Health bar background
    ctx.fillStyle = '#444444';
    ctx.fillRect(barStartX, healthBarY, healthBarWidth, healthBarHeight);

    // Health bar fill
    const hp = healthRatio != null ? healthRatio : 1;
    ctx.fillStyle = '#ff4444';
    ctx.fillRect(barStartX, healthBarY, healthBarWidth * hp, healthBarHeight);

    // Health number on bar
    const healthNum = Math.round(hp * 100);
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 12px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(healthNum.toString(), barStartX + healthBarWidth / 2, healthBarY + healthBarHeight / 2);

    // Stamina bar below health
    const staminaBarY = healthBarY + healthBarHeight + 5;

    // Lightning bolt ⚡
    ctx.font = `${iconSize}px monospace`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillStyle = '#55bbff';
    ctx.fillText('⚡', iconCenterX, staminaBarY + staminaBarHeight / 2);

    // Stamina bar background
    ctx.fillStyle = '#444444';
    ctx.fillRect(barStartX, staminaBarY, healthBarWidth, staminaBarHeight);

    // Stamina bar fill
    ctx.fillStyle = '#55bbff';
    ctx.fillRect(barStartX, staminaBarY, healthBarWidth * staminaRatio, staminaBarHeight);

    // Draw ammo in bottom-right
    ctx.save();
    ctx.font = 'bold 18px monospace';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'bottom';

    if (currentAmmo === 0) {
        ctx.fillStyle = '#ff5555';
    } else if (currentAmmo <= 5) {
        ctx.fillStyle = '#ffcc00';
    } else {
        ctx.fillStyle = '#ffffff';
    }

    const ammoText = currentAmmo + '/' + maxAmmo;
    const ammoTextX = viewWidth - 16;
    const ammoTextY = viewHeight - 16;
    ctx.fillText(ammoText, ammoTextX, ammoTextY);

    // Bullet dot
    const ammoTextWidth = ctx.measureText(ammoText).width;
    ctx.fillStyle = '#ffff44';
    ctx.font = '16px monospace';
    ctx.textAlign = 'right';
    ctx.textBaseline = 'bottom';
    ctx.fillText('●', ammoTextX - ammoTextWidth - 6, ammoTextY);
    ctx.restore();
}

/**
 * Draws a frog using sprite images, with fallback to programmatic drawing.
 * Uses horizontal mirroring based on rotation to indicate facing direction (no rotation applied).
 * @param {CanvasRenderingContext2D} ctx - The 2D canvas rendering context
 * @param {number} x - Screen X position (center)
 * @param {number} y - Screen Y position (center)
 * @param {number} rotation - Frog rotation in radians (used to determine facing direction)
 * @param {number} size - Frog size (radius)
 * @param {boolean} isHopping - Whether the frog is currently hopping
 */
function drawFrog(ctx, x, y, rotation, size, isHopping) {
    // Determine facing: if horizontal component of rotation points left, mirror the sprite
    // cos(rotation) < 0 means facing left
    const facingLeft = Math.cos(rotation) < 0;

    if (frogSpritesLoaded) {
        const img = isHopping ? frogSprites[1] : frogSprites[0];
        if (!img) return;

        // Scale so the larger dimension equals 2 * size (frog diameter), preserve aspect ratio
        const diameter = 2 * size;
        let scaledWidth, scaledHeight;
        if (img.naturalWidth >= img.naturalHeight) {
            scaledWidth = diameter;
            scaledHeight = (img.naturalHeight / img.naturalWidth) * diameter;
        } else {
            scaledHeight = diameter;
            scaledWidth = (img.naturalWidth / img.naturalHeight) * diameter;
        }

        const drawX = x - scaledWidth / 2;
        const drawY = y - scaledHeight / 2;

        if (facingLeft) {
            ctx.save();
            ctx.translate(x, y);
            ctx.scale(-1, 1);
            ctx.translate(-x, -y);
            ctx.drawImage(img, drawX, drawY, scaledWidth, scaledHeight);
            ctx.restore();
        } else {
            ctx.drawImage(img, drawX, drawY, scaledWidth, scaledHeight);
        }
    } else {
        // Fallback: programmatic green blob with eyes
        ctx.save();
        ctx.translate(x, y);
        if (facingLeft) {
            ctx.scale(-1, 1);
        }

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
}

/**
 * Draws the player sprite at the given screen position, scaled and optionally mirrored/flashed.
 * @param {CanvasRenderingContext2D} ctx - The 2D canvas rendering context
 * @param {number} x - Screen X position (center)
 * @param {number} y - Screen Y position (center)
 * @param {number} size - Player radius; sprite larger dimension is scaled to 2 * size
 * @param {number} spriteIndex - Index into playerSprites array (0=stationary, 1=walk1, 2=walk2)
 * @param {boolean} facingLeft - Whether to mirror the sprite horizontally
 * @param {boolean} isFlashing - Whether to apply a red damage overlay
 */
function drawPlayerSprite(ctx, x, y, size, spriteIndex, facingLeft, isFlashing) {
    const img = playerSprites[spriteIndex];
    if (!img) return;

    // Scale so the larger dimension equals 2 * size (player diameter), preserve aspect ratio
    const diameter = 2 * size;
    let scaledWidth, scaledHeight;
    if (img.naturalWidth >= img.naturalHeight) {
        scaledWidth = diameter;
        scaledHeight = (img.naturalHeight / img.naturalWidth) * diameter;
    } else {
        scaledHeight = diameter;
        scaledWidth = (img.naturalWidth / img.naturalHeight) * diameter;
    }

    // Center the image on (x, y)
    const drawX = x - scaledWidth / 2;
    const drawY = y - scaledHeight / 2;

    if (isFlashing) {
        // Use the cached offscreen canvas to draw sprite + red overlay with source-atop compositing
        const reqW = Math.ceil(scaledWidth);
        const reqH = Math.ceil(scaledHeight);
        // Resize cached offscreen canvas only when dimensions are insufficient
        if (_offscreenCanvas.width < reqW || _offscreenCanvas.height < reqH) {
            _offscreenCanvas.width = Math.max(_offscreenCanvas.width, reqW);
            _offscreenCanvas.height = Math.max(_offscreenCanvas.height, reqH);
        }
        const offCtx = _offscreenCtx;
        // Clear the reusable canvas before drawing
        offCtx.clearRect(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);

        // Draw the sprite onto the offscreen canvas
        offCtx.globalCompositeOperation = 'source-over';
        offCtx.drawImage(img, 0, 0, scaledWidth, scaledHeight);

        // Apply red overlay only on top of the sprite pixels
        offCtx.globalCompositeOperation = 'source-atop';
        offCtx.fillStyle = 'rgba(255,0,0,0.4)';
        offCtx.fillRect(0, 0, scaledWidth, scaledHeight);

        // Now draw the offscreen canvas to the main context (with optional mirroring)
        if (facingLeft) {
            ctx.save();
            ctx.translate(x, y);
            ctx.scale(-1, 1);
            ctx.translate(-x, -y);
            ctx.drawImage(_offscreenCanvas, 0, 0, scaledWidth, scaledHeight, drawX, drawY, scaledWidth, scaledHeight);
            ctx.restore();
        } else {
            ctx.drawImage(_offscreenCanvas, 0, 0, scaledWidth, scaledHeight, drawX, drawY, scaledWidth, scaledHeight);
        }
    } else {
        // No flashing — draw directly with optional mirroring
        if (facingLeft) {
            ctx.save();
            ctx.translate(x, y);
            ctx.scale(-1, 1);
            ctx.translate(-x, -y);
            ctx.drawImage(img, drawX, drawY, scaledWidth, scaledHeight);
            ctx.restore();
        } else {
            ctx.drawImage(img, drawX, drawY, scaledWidth, scaledHeight);
        }
    }
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
 * Draws a key-cap style rounded rectangle with a label inside.
 */
function drawKeyCap(ctx, x, y, label, width, height) {
    const radius = 5;
    ctx.save();

    // Background
    ctx.beginPath();
    ctx.roundRect(x, y, width, height, radius);
    ctx.fillStyle = '#2a2a2a';
    ctx.fill();

    // Border
    ctx.strokeStyle = '#888888';
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Subtle inner highlight
    ctx.beginPath();
    ctx.roundRect(x + 1, y + 1, width - 2, height - 2, radius - 1);
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.1)';
    ctx.lineWidth = 1;
    ctx.stroke();

    // Label
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 12px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(label, x + width / 2, y + height / 2);

    ctx.restore();
}

/**
 * Draws a WASD key cluster in arrow-key layout.
 */
function drawWASDCluster(ctx, centerX, y, keySize) {
    const gap = 3;

    // W on top, centered
    drawKeyCap(ctx, centerX - keySize / 2, y, 'W', keySize, keySize);

    // A, S, D on bottom row
    const bottomY = y + keySize + gap;
    const totalWidth = 3 * keySize + 2 * gap;
    const startX = centerX - totalWidth / 2;

    drawKeyCap(ctx, startX, bottomY, 'A', keySize, keySize);
    drawKeyCap(ctx, startX + keySize + gap, bottomY, 'S', keySize, keySize);
    drawKeyCap(ctx, startX + 2 * (keySize + gap), bottomY, 'D', keySize, keySize);
}

/**
 * Draws the start screen with title, start button, and instructions button.
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

    // Frog icon above the title
    const titleY = btnY - 60;
    if (frogSpritesLoaded && frogSprites[0]) {
        const frogImg = frogSprites[0];
        const frogDisplaySize = 80;
        let frogW, frogH;
        if (frogImg.naturalWidth >= frogImg.naturalHeight) {
            frogW = frogDisplaySize;
            frogH = (frogImg.naturalHeight / frogImg.naturalWidth) * frogDisplaySize;
        } else {
            frogH = frogDisplaySize;
            frogW = (frogImg.naturalWidth / frogImg.naturalHeight) * frogDisplaySize;
        }
        const frogX = canvasWidth / 2 - frogW / 2;
        const frogY = titleY - 30 - frogH;
        ctx.drawImage(frogImg, frogX, frogY, frogW, frogH);
    }

    // Title text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Frogmageddon', canvasWidth / 2, titleY);

    // Start button
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, btnY, btnW, btnH);
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, btnY, btnW, btnH);
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Start', btnX + btnW / 2, btnY + btnH / 2);

    // Instructions button (below start button)
    const instrBtnY = btnY + btnH + 20;
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, instrBtnY, btnW, btnH);
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, instrBtnY, btnW, btnH);
    ctx.fillStyle = 'white';
    ctx.font = 'bold 20px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Instructions', btnX + btnW / 2, instrBtnY + btnH / 2);

    // Tagline below buttons
    ctx.fillStyle = '#999999';
    ctx.font = 'italic 14px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    ctx.fillText('Get rid of the frogs invading the office!', canvasWidth / 2, instrBtnY + btnH + 40);
}

/**
 * Draws the instructions screen with key-cap visuals and a Back button.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} canvasWidth - Canvas width
 * @param {number} canvasHeight - Canvas height
 * @param {number} btnX - Back button X position
 * @param {number} btnY - Back button Y position
 * @param {number} btnW - Back button width
 * @param {number} btnH - Back button height
 */
export function drawInstructionsScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    const KEYCAP_SIZE = 32;
    const KEYCAP_WIDE = 56;
    const ROW_HEIGHT = 48;
    const WASD_CLUSTER_H = 2 * KEYCAP_SIZE + 4;
    const BACK_BTN_H = btnH;
    const TOP_MARGIN = 50; // Below score area

    // Calculate total content height for vertical centering
    const titleH = 40;
    const objectiveH = 24;
    const gap = 20;
    const instructionsH = WASD_CLUSTER_H + 20 + 4 * ROW_HEIGHT;
    const totalContentH = titleH + gap + objectiveH + gap + instructionsH + gap + BACK_BTN_H;

    // Center vertically but respect top margin
    const availableH = canvasHeight - TOP_MARGIN;
    let startY = TOP_MARGIN + Math.max(0, (availableH - totalContentH) / 2);

    let currentY = startY;

    // Title
    ctx.fillStyle = 'white';
    ctx.font = 'bold 36px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('How to Play', canvasWidth / 2, currentY + titleH / 2);
    currentY += titleH + gap;

    // Objective
    ctx.fillStyle = '#cccccc';
    ctx.font = 'italic 16px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Get rid of the frogs invading the office!', canvasWidth / 2, currentY + objectiveH / 2);
    currentY += objectiveH + gap;

    // Instructions
    const centerX = canvasWidth / 2;
    const keysX = centerX - 70;
    const labelX = centerX + 40;

    // Row 1: WASD = Move
    drawWASDCluster(ctx, keysX, currentY, KEYCAP_SIZE);
    ctx.fillStyle = '#ffffff';
    ctx.font = '16px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('Move', labelX, currentY + WASD_CLUSTER_H / 2);
    currentY += WASD_CLUSTER_H + 20;

    // Row 2: Mouse + Click = Shoot
    const mouseKeyX = keysX - KEYCAP_WIDE / 2 - 16;
    drawKeyCap(ctx, mouseKeyX, currentY, 'Mouse', KEYCAP_WIDE, KEYCAP_SIZE);
    ctx.fillStyle = '#aaaaaa';
    ctx.font = '14px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('+', mouseKeyX + KEYCAP_WIDE + 12, currentY + KEYCAP_SIZE / 2);
    drawKeyCap(ctx, mouseKeyX + KEYCAP_WIDE + 24, currentY, 'Click', KEYCAP_WIDE, KEYCAP_SIZE);
    ctx.fillStyle = '#ffffff';
    ctx.font = '16px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('Shoot', labelX, currentY + KEYCAP_SIZE / 2);
    currentY += ROW_HEIGHT;

    // Row 3: R = Reload
    drawKeyCap(ctx, keysX - KEYCAP_SIZE / 2, currentY, 'R', KEYCAP_SIZE, KEYCAP_SIZE);
    ctx.fillStyle = '#ffffff';
    ctx.font = '16px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('Reload', labelX, currentY + KEYCAP_SIZE / 2);
    currentY += ROW_HEIGHT;

    // Row 4: Shift = Sprint
    drawKeyCap(ctx, keysX - KEYCAP_WIDE / 2, currentY, 'Shift', KEYCAP_WIDE, KEYCAP_SIZE);
    ctx.fillStyle = '#ffffff';
    ctx.font = '16px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('Sprint', labelX, currentY + KEYCAP_SIZE / 2);
    currentY += ROW_HEIGHT;

    // Row 5: Esc = Pause
    drawKeyCap(ctx, keysX - KEYCAP_WIDE / 2, currentY, 'Esc', KEYCAP_WIDE, KEYCAP_SIZE);
    ctx.fillStyle = '#ffffff';
    ctx.font = '16px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'middle';
    ctx.fillText('Pause', labelX, currentY + KEYCAP_SIZE / 2);
    currentY += ROW_HEIGHT + gap;

    // Back button - centered below instructions
    const backBtnY = currentY;
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, backBtnY, btnW, btnH);
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, backBtnY, btnW, btnH);
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Back', btnX + btnW / 2, backBtnY + btnH / 2);
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
export function drawGameOverScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH, fadeAlpha) {
    const ctx = canvasElement.getContext('2d');

    // Don't clear — keep the last game frame visible as the death scene
    // Draw a dark semi-transparent overlay that fades in
    const alpha = fadeAlpha != null ? fadeAlpha * 0.7 : 0.7;
    ctx.fillStyle = `rgba(0, 0, 0, ${alpha})`;
    ctx.fillRect(0, 0, canvasWidth, canvasHeight);

    // Only show text and button after fade is mostly complete
    const uiAlpha = fadeAlpha != null ? Math.max(0, (fadeAlpha - 0.3) / 0.7) : 1;
    if (uiAlpha <= 0) return;

    ctx.globalAlpha = uiAlpha;

    // Calculate vertical center for title + gap + button as a group
    const titleH = 48;
    const gap = 30;
    const totalH = titleH + gap + btnH;
    const groupStartY = (canvasHeight - totalH) / 2;

    // Game over text in red
    ctx.fillStyle = 'red';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('You died, Game Over.', canvasWidth / 2, groupStartY + titleH / 2);

    // Restart button
    const restartBtnY = groupStartY + titleH + gap;
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, restartBtnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, restartBtnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Restart', btnX + btnW / 2, restartBtnY + btnH / 2);

    ctx.globalAlpha = 1;
}

/**
 * Draws the paused screen overlay.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} canvasWidth - Canvas width
 * @param {number} canvasHeight - Canvas height
 * @param {number} btnX - Resume button X position
 * @param {number} btnY - Resume button Y position
 * @param {number} btnW - Button width
 * @param {number} btnH - Button height
 * @param {number} restartBtnY - Restart button Y position
 */
export function drawPausedScreen(canvasElement, canvasWidth, canvasHeight, btnX, btnY, btnW, btnH, restartBtnY) {
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Semi-transparent dark overlay
    ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
    ctx.fillRect(0, 0, canvasWidth, canvasHeight);

    // Paused text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 48px monospace';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Paused', canvasWidth / 2, canvasHeight / 2 - 60);

    // Resume button rectangle
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, btnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, btnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Resume', btnX + btnW / 2, btnY + btnH / 2);

    // Restart button rectangle
    ctx.fillStyle = '#333333';
    ctx.fillRect(btnX, restartBtnY, btnW, btnH);

    // Button border
    ctx.strokeStyle = 'white';
    ctx.lineWidth = 2;
    ctx.strokeRect(btnX, restartBtnY, btnW, btnH);

    // Button text
    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px monospace';
    ctx.fillText('Restart', btnX + btnW / 2, restartBtnY + btnH / 2);
}

// Audio context for sound effects (created on first use)
let audioCtx = null;

function getAudioContext() {
    if (!audioCtx) {
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }
    return audioCtx;
}

/**
 * Plays a short laser/shoot sound effect using the Web Audio API.
 */
export function playShootSound() {
    const ctx = getAudioContext();
    const oscillator = ctx.createOscillator();
    const gainNode = ctx.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(ctx.destination);

    oscillator.type = 'square';
    oscillator.frequency.setValueAtTime(600, ctx.currentTime);
    oscillator.frequency.exponentialRampToValueAtTime(200, ctx.currentTime + 0.1);

    gainNode.gain.setValueAtTime(0.3, ctx.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.1);

    oscillator.start(ctx.currentTime);
    oscillator.stop(ctx.currentTime + 0.1);
}

/**
 * Plays a battery recharge sound effect — a rising tone that builds in intensity.
 */
export function playReloadSound() {
    const ctx = getAudioContext();

    // Rising tone oscillator
    const oscillator = ctx.createOscillator();
    const gainNode = ctx.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(ctx.destination);

    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(80, ctx.currentTime);
    oscillator.frequency.exponentialRampToValueAtTime(800, ctx.currentTime + 1.2);

    gainNode.gain.setValueAtTime(0.075, ctx.currentTime);
    gainNode.gain.linearRampToValueAtTime(0.175, ctx.currentTime + 1.0);
    gainNode.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 1.3);

    oscillator.start(ctx.currentTime);
    oscillator.stop(ctx.currentTime + 1.3);

    // Electric hum layer
    const hum = ctx.createOscillator();
    const humGain = ctx.createGain();

    hum.connect(humGain);
    humGain.connect(ctx.destination);

    hum.type = 'sawtooth';
    hum.frequency.setValueAtTime(60, ctx.currentTime);
    hum.frequency.exponentialRampToValueAtTime(400, ctx.currentTime + 1.2);

    humGain.gain.setValueAtTime(0.025, ctx.currentTime);
    humGain.gain.linearRampToValueAtTime(0.06, ctx.currentTime + 0.9);
    humGain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 1.3);

    hum.start(ctx.currentTime);
    hum.stop(ctx.currentTime + 1.3);
}

/**
 * Plays a short alert/damage sound effect — a quick descending buzz.
 */
export function playDamageSound() {
    const ctx = getAudioContext();
    const oscillator = ctx.createOscillator();
    const gainNode = ctx.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(ctx.destination);

    oscillator.type = 'square';
    oscillator.frequency.setValueAtTime(400, ctx.currentTime);
    oscillator.frequency.exponentialRampToValueAtTime(100, ctx.currentTime + 0.2);

    gainNode.gain.setValueAtTime(0.4, ctx.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.25);

    oscillator.start(ctx.currentTime);
    oscillator.stop(ctx.currentTime + 0.25);
}

/**
 * Plays a short positive chime sound when the player scores.
 */
export function playScoreSound() {
    const ctx = getAudioContext();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.type = 'sine';
    osc.frequency.setValueAtTime(520, ctx.currentTime);
    osc.frequency.setValueAtTime(780, ctx.currentTime + 0.08);

    gain.gain.setValueAtTime(0.075, ctx.currentTime);
    gain.gain.setValueAtTime(0.075, ctx.currentTime + 0.12);
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.2);

    osc.start(ctx.currentTime);
    osc.stop(ctx.currentTime + 0.2);
}
