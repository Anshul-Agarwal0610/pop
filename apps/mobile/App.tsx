import { StatusBar } from 'expo-status-bar';
import {
  Pressable,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { API_BASE_URL } from './src/config/api';

export default function App() {
  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="dark" />
      <ScrollView contentContainerStyle={styles.container}>
        <View style={styles.hero}>
          <Text style={styles.eyebrow}>Pollify Mobile</Text>
          <Text style={styles.title}>Vote daily. Keep the streak alive.</Text>
          <Text style={styles.subtitle}>
            A native Android and iOS foundation for Pollify's mobile-first poll
            experience.
          </Text>
        </View>

        <View style={styles.panel}>
          <Text style={styles.panelTitle}>App foundation</Text>
          <View style={styles.row}>
            <Text style={styles.label}>Runtime</Text>
            <Text style={styles.value}>Expo + React Native</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Backend API</Text>
            <Text style={styles.value}>{API_BASE_URL}</Text>
          </View>
          <View style={styles.row}>
            <Text style={styles.label}>Target</Text>
            <Text style={styles.value}>Android first, iOS ready</Text>
          </View>
        </View>

        <View style={styles.actionPanel}>
          <Text style={styles.actionTitle}>Next build steps</Text>
          <Text style={styles.actionCopy}>
            Connect auth, poll feed, voting, streaks, and leaderboard screens
            against the existing ASP.NET Core API.
          </Text>
          <Pressable style={styles.button}>
            <Text style={styles.buttonText}>Ready for feature screens</Text>
          </Pressable>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F7F5EF',
  },
  container: {
    flexGrow: 1,
    padding: 24,
    gap: 18,
  },
  hero: {
    paddingTop: 36,
    paddingBottom: 16,
  },
  eyebrow: {
    color: '#B0413E',
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0,
    marginBottom: 10,
    textTransform: 'uppercase',
  },
  title: {
    color: '#222222',
    fontSize: 36,
    fontWeight: '800',
    letterSpacing: 0,
    lineHeight: 42,
  },
  subtitle: {
    color: '#54514A',
    fontSize: 17,
    lineHeight: 25,
    marginTop: 14,
  },
  panel: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E6E0D4',
    borderRadius: 8,
    borderWidth: 1,
    padding: 18,
    gap: 14,
  },
  panelTitle: {
    color: '#222222',
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: 0,
    marginBottom: 2,
  },
  row: {
    borderTopColor: '#EEE8DC',
    borderTopWidth: 1,
    gap: 4,
    paddingTop: 12,
  },
  label: {
    color: '#756F63',
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0,
    textTransform: 'uppercase',
  },
  value: {
    color: '#26231F',
    fontSize: 16,
    fontWeight: '600',
    lineHeight: 22,
  },
  actionPanel: {
    backgroundColor: '#233D4D',
    borderRadius: 8,
    padding: 20,
    gap: 14,
  },
  actionTitle: {
    color: '#FFFFFF',
    fontSize: 20,
    fontWeight: '800',
    letterSpacing: 0,
  },
  actionCopy: {
    color: '#DDE7EA',
    fontSize: 16,
    lineHeight: 23,
  },
  button: {
    alignItems: 'center',
    backgroundColor: '#F4D35E',
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 13,
  },
  buttonText: {
    color: '#1F2B32',
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0,
  },
});
