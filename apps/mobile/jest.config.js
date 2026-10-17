module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: ['**/*.test.ts'],
  transform: { '^.+\\.tsx?$': ['ts-jest', { tsconfig: { jsx: 'react-jsx' } }] },
  moduleNameMapper: {
    '\\.(png|jpe?g|gif|webp)$': '<rootDir>/src/test/file.jest.js',
    '^\\.\\./\\.\\./\\.\\./\\.\\./design-tokens/tokens\\.json$': '<rootDir>/src/theme/tokens.jest.js',
    '^expo-image-picker$': '<rootDir>/src/test/expo-image-picker.jest.js',
    '^expo-video$': '<rootDir>/src/test/expo-video.jest.js',
    '^expo-sensors$': '<rootDir>/src/test/expo-sensors.jest.js',
    '^react-native-svg$': '<rootDir>/src/test/react-native-svg.jest.js',
    '^react-native-webview$': '<rootDir>/src/test/react-native-webview.jest.js',
  },
};
