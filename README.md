# SoftielRemote

## Project Overview

SoftielRemote, AnyDesk/TeamViewer benzeri bir uzaktan erişim çözümüdür. Kullanıcılar, benzersiz bir cihaz ID'si ile bağlanarak ekran görüntüsü alabilir ve cihazı kontrol edebilirler.

**Temel Hedefler:**
- **ID ile bağlan**: Basit ve hızlı bağlantı kurma
- **Ekran gör + kontrol et**: Gerçek zamanlı ekran paylaşımı ve uzaktan kontrol
- **Sürpriz kesinti yok**: Trial/lisans bitince mevcut oturumlar devam eder, sadece yeni oturum oluşturma engellenir
- **Her ağda çalışır**: Aynı LAN veya farklı network fark etmez; WebRTC + STUN/TURN ile NAT traversal
- **Cross-platform**: Windows ⇄ Windows, Windows ⇄ macOS, macOS ⇄ macOS (host/agent + viewer)
- **Production-ready**: İlk ticari sürüm odağı stabil + güvenli + küçük teknik servis/freelance kullanımı

## Core Principles

### Production-First Approach

**KATI KURAL**: Proje sadece production-ready çözümler kullanır. Local-only, dev-only, mock, loopback, hardcoded IP, insecure flags, debug transports **YASAKTIR**. Eğer bir şey production koşullarında çalışmıyorsa, alternatif çözüm eklenmez; ana çözüm düzeltilir.

### Architectural Freeze

Tüm mimari, teknoloji, UX, iş ve lisanslama kararları **FİNAL**dir. Alternatif stack, engine, protokol veya UI paradigması önerilmez. Ödeme sağlayıcı seçimi tek istisnadır ve son fazda ele alınır.

### Security & Privacy

- **Onay akışı zorunlu**: Tüm bağlantılar host tarafından onaylanmalı
- **Güvenlik katmanları**: Rate limiting, abuse detection, device fingerprinting
- **PII koruması**: Loglarda PII yok; device fingerprint hash kullanılır
- **Secrets yönetimi**: Asla repoya konmaz (Azure Key Vault / App Settings)

### Network Architecture

- **Signaling**: Her zaman backend üzerinden (SignalR)
- **Media + Input**: Her zaman WebRTC
- **ICE servers**: STUN (public) + TURN UDP/TCP fallback + TURNS (TLS) corporate ağlar için
- **NAT traversal**: WebRTC + STUN/TURN ile her ağda çalışır

### Trial & Licensing Model

- **Trial abuse önleme**: Soft device fingerprint (hash), IP/ASN sinyali, email doğrulama, risk score
- **Trial bitince**: Mevcut oturumlar devam eder, sadece yeni oturum engellenir
- **Token modeli**: 1 token = 1 session (pay-as-you-go)
- **Paketler**: Starter / Pro / Service / Self-hosted (detaylar `docs/packages.md`)

## Architecture Summary

### Backend & Infrastructure (Azure "tek çatı")

- **Backend**: .NET 8 ASP.NET Core (REST + SignalR)
- **Database**: Azure Database for PostgreSQL (Flexible Server)
- **Cache/State**: Azure Cache for Redis
- **TURN Server**: Coturn → Azure VM (Ubuntu) üzerinde
- **Observability**: Serilog + OpenTelemetry (isteğe bağlı)

### UI

- **Tek UI**: Flutter Desktop (Windows + macOS)
- **State Management**: Riverpod
- **UI Paradigm**: Browser-style tab sistemi
  - Default: Home tab açık
  - Yeni session başlayınca: otomatik "Viewer" tab açılır
  - Ayarlar / Billing / Account: hamburger menüden yeni tab olarak açılır

### WebRTC Stack

- **Flutter viewer**: `flutter_webrtc`
- **Agent (Windows/macOS)**: libdatachannel tabanlı engine
  - **Önerilen**: DataChannelDotnet (C# wrapper over libdatachannel)
  - **Gerekçe**: Native stack + media tracks + cross-platform

### Agent Architecture

- **Agent.Core**: Ortak .NET 8 class library (session lifecycle, signaling, auth, config)
- **Agent.Windows**: .NET 8 (Windows Graphics Capture / Desktop Duplication, SendInput, WebRTC engine)
- **Agent.Mac**: macOS Host Agent (Swift) - ScreenCaptureKit capture, CGEvent input injection, libdatachannel WebRTC

### Session Lifecycle (State Machine)

```
[Created] → [PendingApproval] → [Accepted] → [NegotiatingWebRTC] → [Connected] → [Ended]
```

- **PendingApproval**: 60s timeout → Failed
- **NegotiatingWebRTC**: 45s timeout → Failed
- **Trial/lisans bitince**: Connected session bitene kadar devam, sadece new session engellenir

## Development vs Production

### Production-Only Rule (NON-NEGOTIABLE)

- **Sadece production-ready çözümler**: Local-only, dev-only, mock, loopback, hardcoded IP, insecure flags, debug transports **YASAKTIR**
- **Deneysel teknolojiler yok**: Tüm teknolojiler production-grade olmalı
- **Tüm akışlar production seviyesinde**: Networking, security, media, auth, storage, permission flows

### Development Standards

- **Backend**: Clean-ish architecture (Controllers → Services → Repos), DTO contract stable
- **Flutter**: Riverpod + feature-based folder (home/, session/, settings/, billing/)
- **Kod tekrarı**: Olabildiğince az
- **Kodlama standartları**: Tüm kod standartlara uygun

### Localization

- **Desteklenen diller**: 12 dil (English, Türkçe, Deutsch, Français, Español, Italiano, Português, Русский, 中文, 日本語, 한국어, العربية)
- **Hardcoded string yok**: Flutter `intl` + `flutter_localizations` kullanır
- **Tüm diller zorunlu**: Yeni UI ekranı/string eklendiğinde tüm diller güncellenir
- **Varsayılan dil**: Sistem dili ("Automatic")

## Phased Development Model

Proje, adım adım, her adım sonunda çalışır + test edilir şekilde ilerler.

### Faz A – Repo düzeni ve temizlik
- TCP stream kodlarını kaldır
- Ortak DTO kontratını sabitle (SoftielRemote.Core)
- Backend deploy pipeline'ı düzelt (Azure)

### Faz B – TURN + ICE servers
- Azure VM'de coturn kur ve test et
- Backend TURN credential endpoint'ini ekle
- Flutter ve Agent ICE config'i backend'den alsın

### Faz C – WebRTC Video + Input "tamam"
- Agent WebRTC engine'i DataChannelDotnet ile finalize et
- Flutter viewer: video render + input send
- Session lifecycle + timeout + retry

### Faz D – macOS Host Agent (ilk production)
- Swift mac agent: ScreenCaptureKit + input + WebRTC + signaling
- Backend "platform" alanı ekle

### Faz E – Security hardening
- Approval flow zorunlu
- Permission levels
- Rate limiting + abuse signals

### Faz F – Auth + Trial
- Email signup/login + JWT
- Trial tracking (1 cihaz = 1 trial)
- Trial bittiğinde: new session engeli

### Faz G – Packaging & Auto-update
- Windows installer (MSIX/WiX) + auto-start
- macOS notarization/signing
- Auto-update stratejisi (Squirrel/WinGet + Sparkle)

### Faz H – Billing (en son)
- Subscription + Token (1 token = 1 session)
- Türkiye/global PSP kararı

## Non-Goals

- **TCP Stream**: Tamamen kaldırıldı (NAT arkasında çalışmaz, WebRTC ile duplicate yol, prod'da attack surface)
- **Alternatif teknolojiler**: Mimari kararlar final, alternatif stack/engine/protokol önerilmez
- **Ek diller**: İlk production release'de sadece 12 dil desteklenir
- **Dinamik içerik çevirisi**: Localization sadece UI metinleri içindir
- **Local-only çözümler**: Production-ready olmayan hiçbir çözüm kabul edilmez
- **Deneysel özellikler**: Production-grade olmayan özellikler eklenmez

---

**Not**: Detaylı teknik kararlar ve uygulama planı için `.cursor/rules/cursorrules.mdc` dosyasına bakınız.










