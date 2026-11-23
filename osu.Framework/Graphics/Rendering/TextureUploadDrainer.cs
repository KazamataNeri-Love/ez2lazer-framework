// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Shared drain path for a texture's local <see cref="ITextureUpload"/> queue.
    /// Respects the renderer's per-frame region / pixel / time budget so one atlas cannot dump thousands of uploads in a single draw call.
    /// </summary>
    internal static class TextureUploadDrainer
    {
        public static void LogIfQueueLarge(INativeTexture texture, int queueCount)
        {
            if (queueCount < 100 || queueCount % 100 != 0)
                return;

            string name = !string.IsNullOrEmpty(texture.Label) ? texture.Label : texture.Identifier;
            Logger.Log($"Texture [{name}] {texture.Width}x{texture.Height} upload queue is large ({queueCount})", LoggingTarget.Performance);
        }

        /// <summary>
        /// Drains pending uploads from <paramref name="uploadQueue"/> within the renderer's per-frame upload budget.
        /// </summary>
        /// <param name="uploadQueue">The texture's pending upload queue.</param>
        /// <param name="renderer">The active renderer tracking per-frame upload budgets.</param>
        /// <param name="bypassQueueing">Whether all queued uploads for this texture bypass per-frame budget limits.</param>
        /// <param name="requireInitialUpload">
        /// Whether this texture currently has no GPU backing resource yet. When <c>true</c>, the first upload is allowed to
        /// bypass the remaining per-frame budget (while still accounting into the frame counters) so binding the texture
        /// cannot observe an uninitialised backend resource; any subsequent uploads still obey the budget.
        /// </param>
        /// <param name="performUpload">Callback that uploads a single dequeued <see cref="ITextureUpload"/>.</param>
        public static int Drain(Queue<ITextureUpload> uploadQueue, IRenderer renderer, bool bypassQueueing, bool requireInitialUpload, Action<ITextureUpload> performUpload)
        {
            int uploaded = 0;

            while (true)
            {
                ITextureUpload upload;

                lock (uploadQueue)
                {
                    if (uploadQueue.Count == 0)
                        break;

                    upload = uploadQueue.Peek();
                    int pixels = Math.Max(0, upload.Bounds.Width * upload.Bounds.Height);
                    bool bypassBudget = bypassQueueing || (uploaded == 0 && requireInitialUpload);

                    if (!renderer.TryBeginTextureUpload(pixels, bypassBudget))
                        break;

                    uploadQueue.Dequeue();
                }

                using (upload)
                    performUpload(upload);

                uploaded++;
            }

            return uploaded;
        }
    }
}
