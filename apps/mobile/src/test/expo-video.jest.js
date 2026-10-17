const React = require('react');
module.exports = {
  useVideoPlayer: () => ({ loop: false }),
  VideoView: props => React.createElement('VideoView', props),
};
