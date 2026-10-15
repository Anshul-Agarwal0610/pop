module.exports = {
  requestCameraPermissionsAsync: async () => ({ granted: true }),
  requestMediaLibraryPermissionsAsync: async () => ({ granted: true }),
  launchCameraAsync: async () => ({ canceled: true, assets: null }),
  launchImageLibraryAsync: async () => ({ canceled: true, assets: null }),
};
