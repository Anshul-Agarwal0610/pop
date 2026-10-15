import { describe, expect, test } from '@jest/globals';
import { ballotChoices, ballotTarget, measuredBallotTarget } from './ballot';
import type { ApiPollOption } from '../types/poll';
const option = (id: number, text: string) => ({ id, text } as ApiPollOption);
describe('ballot mapping', () => {
  test('keeps real IDs when the server orders support first', () => {
    expect(ballotChoices([option(9, 'Support'), option(4, 'Against')])?.map(value => value.id)).toEqual([4, 9]);
    expect(ballotChoices([option(4, 'Down'), option(9, 'Up')])?.map(value => value.label)).toEqual(['Against', 'Support']);
  });
  test('maps the web creator Yes/No pair to the matching ballot sides', () => {
    expect(ballotChoices([option(12, 'No'), option(13, 'Yes')])?.map(value => [value.id, value.label])).toEqual([[12, 'Against'], [13, 'Support']]);
  });
  test('uses stable sides when option text contains generated position statements', () => {
    const support = { ...option(21, 'Expand the programme statewide'), side: 'Up' };
    const against = { ...option(22, 'Keep the programme limited'), side: 'Against' };
    expect(ballotChoices([support, against])?.map(value => [value.label, value.statement])).toEqual([
      ['Against', 'Keep the programme limited'],
      ['Support', 'Expand the programme statewide'],
    ]);
  });
  test('does not relabel unrelated or multi-option answers', () => {
    expect(ballotChoices([option(1, 'Tea'), option(2, 'Coffee')])).toBeNull();
    expect(ballotChoices([option(1, 'Support'), option(2, 'Against'), option(3, 'Other')])).toBeNull();
  });
  test('requires the ballot to be released inside a box', () => {
    expect(ballotTarget(50, 220, 300)).toBe(0);
    expect(ballotTarget(250, 220, 300)).toBe(1);
    expect(ballotTarget(150, 220, 300)).toBeNull();
    expect(ballotTarget(50, 130, 300)).toBeNull();
    expect(ballotTarget(-10, 220, 300)).toBeNull();
  });
});

test('measured drop targets follow scrolling, screen offsets, and box edges', () => {
  const boxes = [{ x: 24, y: 410, width: 140, height: 146 }, { x: 180, y: 410, width: 140, height: 146 }];
  expect(measuredBallotTarget(80, 430, boxes)).toBe(0);
  expect(measuredBallotTarget(240, 540, boxes)).toBe(1);
  expect(measuredBallotTarget(240, 402, boxes)).toBe(1);
  expect(measuredBallotTarget(172, 450, boxes)).toBeNull();
  expect(measuredBallotTarget(240, 200, boxes)).toBeNull();
  expect(measuredBallotTarget(240, 240, boxes.map(box => ({ ...box, y: box.y - 300 })))).toBe(1);
  expect(measuredBallotTarget(240, 240, [])).toBeNull();
});
