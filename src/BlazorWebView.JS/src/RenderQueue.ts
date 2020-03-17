import { renderBatch } from '@browserjs/Rendering/Renderer';
import { OutOfProcessRenderBatch } from '@browserjs/Rendering/RenderBatch/OutOfProcessRenderBatch';
import * as ipc from './IPC';

export class RenderQueue {
    private static instance: RenderQueue;

    private nextBatchId = 2;

    private fatalError?: string;

    public browserRendererId: number;

    public constructor(browserRendererId: number) {
        this.browserRendererId = browserRendererId;
    }

    public static getOrCreate(): RenderQueue {
        if (!RenderQueue.instance) {
            RenderQueue.instance = new RenderQueue(0);
        }

        return this.instance;
    }

    public async processBatch(receivedBatchId: number, batchData: Uint8Array): Promise<void> {
        if (receivedBatchId < this.nextBatchId) {
            await this.completeBatch(receivedBatchId);
            return;
        }

        if (receivedBatchId > this.nextBatchId) {
            if (this.fatalError) {
                console.log(`Received a new batch ${receivedBatchId} but errored out on a previous batch ${this.nextBatchId - 1}`);
                await ipc.send('OnRenderCompleted', [this.nextBatchId - 1, this.fatalError.toString()]);
                return;
            }
            return;
        }

        try {
            this.nextBatchId++;
            renderBatch(this.browserRendererId, new OutOfProcessRenderBatch(batchData));
            await this.completeBatch(receivedBatchId);
        } catch (error) {
            this.fatalError = error.toString();
            console.error(`There was an error applying batch ${receivedBatchId}.`);

            // If there's a rendering exception, notify server *and* throw on client
            ipc.send('OnRenderCompleted', [receivedBatchId, error.toString()]);
            throw error;
        }
    }

    public getLastBatchid(): number {
        return this.nextBatchId - 1;
    }

    private async completeBatch(batchId: number): Promise<void> {
        try {
            await ipc.send('OnRenderCompleted', [batchId, null]);
        } catch {
            console.warn(`Failed to deliver completion notification for render '${batchId}'.`);
        }
    }
}
