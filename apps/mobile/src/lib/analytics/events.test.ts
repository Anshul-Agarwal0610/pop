import { describe, expect, it } from '@jest/globals';

import { sanitize } from './events';

describe('mobile analytics privacy contract', () => {
  it('rejects selected answers', () => expect(() => sanitize('game_round_completed', { round_id: 'r', surface: 'feed', outcome: 'voted', xp_awarded: 1, selected_option_id: 2 } as never)).toThrow(/not allowed/));
});
