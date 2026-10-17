const { withAndroidStyles, AndroidConfig } = require('expo/config-plugins');

// Keep a black system-navigation background on Android versions that render
// an opaque bar. On edge-to-edge Android, App.tsx paints the bottom inset black.
module.exports = function withBlackNavigationBar(config) {
  return withAndroidStyles(config, config => {
    for (const [name, value] of Object.entries({
      'android:navigationBarColor': '#000000',
      'android:windowLightNavigationBar': 'false',
      'android:enforceNavigationBarContrast': 'false',
      'android:navigationBarDividerColor': '#000000',
    })) {
      config.modResults = AndroidConfig.Styles.assignStylesValue(config.modResults, {
        add: true, parent: AndroidConfig.Styles.getAppThemeGroup(), name, value,
      });
    }
    return config;
  });
};
