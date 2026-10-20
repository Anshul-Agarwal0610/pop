import { createVideoPlayer } from 'expo-video';
import * as ImageManipulator from 'expo-image-manipulator';

export async function createVideoPoster(uri: string): Promise<string> {
  const player = createVideoPlayer(uri);
  try {
    if (player.status !== 'readyToPlay') {
      await new Promise<void>((resolve, reject) => {
        const timer = setTimeout(() => { listener.remove(); reject(new Error('Video preview timed out.')); }, 10000);
        const listener = player.addListener('statusChange', event => {
          if (event.status === 'readyToPlay' || event.status === 'error') {
            clearTimeout(timer);
            listener.remove();
            event.status === 'error' ? reject(new Error('Video preview unavailable.')) : resolve();
          }
        });
      });
    }
    const [thumbnail] = await player.generateThumbnailsAsync(0.5, { maxWidth: 1280 });
    if (!thumbnail) throw new Error('Video preview unavailable.');
    const context = ImageManipulator.ImageManipulator.manipulate(thumbnail);
    const rendered = await context.renderAsync();
    const image = await rendered.saveAsync({ format: ImageManipulator.SaveFormat.JPEG, compress: 0.78 });
    return image.uri;
  } finally {
    player.release();
  }
}
