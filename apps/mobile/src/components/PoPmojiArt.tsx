import Svg, { Circle, Ellipse, Path, Rect } from 'react-native-svg';

export type PollReaction = 'big_yes' | 'thinking' | 'no_way' | 'wow';

export const reactions: Array<{ id: PollReaction; label: string; caption: string }> = [
  { id: 'big_yes', label: 'Big yes', caption: 'All in' },
  { id: 'thinking', label: 'Wait…', caption: 'Hear me out' },
  { id: 'no_way', label: 'No way', caption: 'Hard pass' },
  { id: 'wow', label: 'Wow', caption: 'Did that happen?' },
];

const ink = '#252033';

export function PoPmojiArt({ id, size = 70 }: { id: PollReaction; size?: number }) {
  return <Svg width={size} height={size} viewBox="0 0 100 100" accessibilityLabel={`PoPmoji ${reactions.find(item => item.id === id)?.label}`}>
    {id === 'big_yes' && <>
      <Path d="M17 43C10 28 23 11 43 13c9-10 28-5 32 9 17 4 20 23 8 34 5 18-9 34-28 31-19 9-40-2-40-20-8-7-7-18 2-24Z" fill="#CFFF70" stroke={ink} strokeWidth="3" />
      <Path d="M70 27 75 8c1-5 9-4 8 2l-2 13 6-10c3-5 10-1 7 4L84 40" fill="#CFFF70" stroke={ink} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
      <Path d="M39 46c2-5 7-5 9 0m16 0c2-5 7-5 9 0" fill="none" stroke={ink} strokeWidth="3.5" strokeLinecap="round" />
      <Path d="M40 61c8 12 23 13 33 0" fill="#FFFFFF" stroke={ink} strokeWidth="3.5" strokeLinecap="round" />
      <Circle cx="31" cy="59" r="5" fill="#FF8B91" opacity=".7" /><Circle cx="80" cy="59" r="5" fill="#FF8B91" opacity=".7" />
      <Path d="m13 14 2-7m-8 12-6-2m86 63 5 5" stroke="#F56E74" strokeWidth="3" strokeLinecap="round" />
    </>}
    {id === 'thinking' && <>
      <Path d="M17 48c-9-16 0-31 18-34 10-9 29-6 36 7 18 0 27 18 17 32 4 17-10 30-27 29-20 11-45 1-48-16-7-7-5-14 4-18Z" fill="#D9C7FA" stroke={ink} strokeWidth="3" />
      <Path d="M32 42c5-5 10-4 14-1m18-1c4-2 9-1 13 2" stroke={ink} strokeWidth="3.5" fill="none" strokeLinecap="round" />
      <Circle cx="40" cy="50" r="3.5" fill={ink} /><Circle cx="70" cy="50" r="3.5" fill={ink} />
      <Path d="M50 68c5-3 10-3 15 0" fill="none" stroke={ink} strokeWidth="3.5" strokeLinecap="round" />
      <Path d="M66 91c-4-9-2-18 3-23 4-5 10-1 7 4l-3 5 8-5c6-3 10 4 4 8L72 91Z" fill="#BBA1EB" stroke={ink} strokeWidth="3" strokeLinejoin="round" />
      <Path d="M11 24c1-8 8-13 16-13" fill="none" stroke="#8B67C7" strokeWidth="3" strokeLinecap="round" />
    </>}
    {id === 'no_way' && <>
      <Path d="M23 19c8-8 41-9 54 0 13 10 14 44 4 57-10 12-52 13-64 2C5 67 8 32 23 19Z" fill="#A8E8ED" stroke={ink} strokeWidth="3" />
      <Path d="m29 41 17 5m29-5-17 5" stroke={ink} strokeWidth="4" strokeLinecap="round" />
      <Circle cx="37" cy="53" r="3.5" fill={ink} /><Circle cx="66" cy="53" r="3.5" fill={ink} />
      <Path d="M43 69c5-5 14-5 19 0" fill="none" stroke={ink} strokeWidth="4" strokeLinecap="round" />
      <Path d="m10 66 23 17 18-10m40-7L68 83 50 73" fill="none" stroke={ink} strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" />
      <Path d="m13 8 11 8m62-8-11 8" stroke="#FF777D" strokeWidth="3" strokeLinecap="round" />
    </>}
    {id === 'wow' && <>
      <Path d="m50 3 9 15 17-8-1 18 18 3-11 14 12 15-18 3 1 18-18-8-9 15-9-15-18 8 1-18-18-3 12-15L7 31l18-3-1-18 17 8Z" fill="#FFCE78" stroke={ink} strokeWidth="3" strokeLinejoin="round" />
      <Ellipse cx="35" cy="44" rx="4" ry="7" fill={ink} /><Ellipse cx="65" cy="44" rx="4" ry="7" fill={ink} />
      <Ellipse cx="50" cy="65" rx="11" ry="13" fill="#F37072" stroke={ink} strokeWidth="3" />
      <Path d="m25 33 11-4m28 0 11 4" stroke={ink} strokeWidth="3" strokeLinecap="round" />
      <Rect x="7" y="4" width="6" height="6" rx="1" fill="#F37072" transform="rotate(-15 10 7)" />
      <Circle cx="91" cy="87" r="4" fill="#A892F4" />
    </>}
  </Svg>;
}
