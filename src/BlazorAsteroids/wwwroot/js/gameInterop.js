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
 * Renders a full frame: background with camera offset, then player.
 * @param {HTMLCanvasElement} canvasElement - The canvas DOM element
 * @param {number} cameraX - Camera top-left X in world space
 * @param {number} cameraY - Camera top-left Y in world space
 * @param {number} playerX - Player X position in world space
 * @param {number} playerY - Player Y position in world space
 * @param {number} rotation - Player rotation in radians
 * @param {number} size - Player size for triangle dimensions
 */
export function renderFrame(canvasElement, cameraX, cameraY, playerX, playerY, rotation, size) {
    const ctx = canvasElement.getContext('2d');
    const viewWidth = canvasElement.width;
    const viewHeight = canvasElement.height;

    // Clear the canvas
    ctx.clearRect(0, 0, viewWidth, viewHeight);

    // Draw the background image, offset by camera position
    if (backgroundLoaded && backgroundImage) {
        // The background image is drawn at world scale.
        // We need to map the camera's view of the world onto the canvas.
        // Source rect: the portion of the image the camera is looking at.
        // The image covers the full world, so we sample proportionally.
        const imgW = backgroundImage.naturalWidth;
        const imgH = backgroundImage.naturalHeight;

        // World dimensions — derived from how much of the image we show
        // We assume the image maps 1:1 to world coordinates scaled by image/world ratio
        const worldWidth = 2000;
        const worldHeight = 1500;

        // Source coordinates in the image
        const sx = (cameraX / worldWidth) * imgW;
        const sy = (cameraY / worldHeight) * imgH;
        const sw = (viewWidth / worldWidth) * imgW;
        const sh = (viewHeight / worldHeight) * imgH;

        ctx.drawImage(backgroundImage, sx, sy, sw, sh, 0, 0, viewWidth, viewHeight);
    } else {
        // Fallback: dark background if image not loaded yet
        ctx.fillStyle = '#1a1a2e';
        ctx.fillRect(0, 0, viewWidth, viewHeight);
    }

    // Convert player world position to screen position
    const screenX = playerX - cameraX;
    const screenY = playerY - cameraY;

    // Draw the player
    ctx.save();
    ctx.translate(screenX, screenY);
    ctx.rotate(rotation);

    // Draw triangle ship: nose pointing right (+X direction)
    ctx.beginPath();
    ctx.moveTo(size, 0);                    // Nose
    ctx.lineTo(-size * 0.7, -size * 0.6);   // Top-left wing
    ctx.lineTo(-size * 0.7, size * 0.6);    // Bottom-left wing
    ctx.closePath();

    ctx.fillStyle = 'cyan';
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
