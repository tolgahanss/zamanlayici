# Zamanlayici - PC Kapatma & Yeniden Baslatma

Windows icin gelistirilmis, modern arayuzlu PC kapatma, yeniden baslatma ve akilli izleme zamanlayici uygulamasi.

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_Framework_4.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

---

## Ozellikler

### Zamanlayici Modu
Belirlediginiz sure sonunda bilgisayari otomatik olarak kapatir veya yeniden baslatir.
- Hizli ayar butonlari (15dk, 30dk, 45dk, 1 saat, 1.5 saat, 2 saat)
- Ozel sure girisi (saat:dakika:saniye)
- Geri sayim goruntusu ve ilerleme halkasi
- Windows `shutdown` komutunu kullanir

### Hareketsizlik Modu
Mouse ve klavye hareketlerini izler. Belirlediginiz sure boyunca hic hareket olmazsa bilgisayari kapatir veya yeniden baslatir.
- Windows API `GetLastInputInfo` ile sistem bosta kalma suresi izlenir
- Herhangi bir mouse/klavye hareketi sayaci otomatik sifirlar
- Canli bosta kalma suresi gosterimi

### Akilli Izleyici Modu
Arka planda belirli bir programi veya ag (internet) kullanimini izler. Kosul saglandiginda PC otomatik olarak kapanir.
- **Islem (Program) Izle**: Kapanmasini beklediginiz programin adini (ornek: `steam`, `IDMan`, `chrome`) girin. O program kapatildiginda bilgisayar kapanir.
- **Ag (Internet) Izle**: Internet hizi (indirme) belirlenen KB/s altina duserse (ornek: `100` KB/s) bilgisayar kapanir. Ozellikle uzun suren indirmeler bittiginde PC'yi kapatmak icin idealdir.
- `NetworkInterface` uzerinden ag verileri hesaplanir.

### Genel
- **Kapat** veya **Yeniden Baslat** secimi
- **Modern koyu tema** arayuz
- **Owner-drawn yuvarlatilmis butonlar** ve gradient efektler
- **Animasyonlu ilerleme halkasi** (parlayan nokta efekti)
- **Iptal** butonu ile geri sayim veya izlemeyi durdurma
- **Sistem Tepsisine Kucultme**: Arka planda calisirken rahatsiz etmez (sag alt koseye kuculur)
- **Cikis korumasi** - uygulama kapatilirken aktif zamanlayici uyarisi
- Ek yazilim gerektirmez, tek `.exe` dosyasi

---

## Kurulum

### Hazir EXE (Onerilir)
1. [Releases](../../releases) sayfasindan `Zamanlayici.exe` dosyasini indirin
2. Cift tikla calistirin

### Kaynak Koddan Derleme
Ek bir araca gerek yok, Windows'un dahili C# derleyicisi kullanilir:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:app.ico /out:Zamanlayici.exe /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Zamanlayici.cs
```

---

## Kullanim

### Zamanlayici Modu
1. **"Zamanlayici"** sekmesini secin (varsayilan)
2. **"Bilgisayari Kapat"** veya **"Yeniden Baslat"** secin
3. Hizli ayar butonlarindan birini secin veya ozel sure girin
4. **"BASLAT"** butonuna tiklayin

### Hareketsizlik Modu
1. **"Hareketsizlik"** sekmesine tiklayin
2. Bosta kalma esik suresini belirleyin (orn: 30 dakika)
3. **"BASLAT"** butonuna tiklayin
4. Uygulama mouse/klavye hareketlerini izlemeye baslar ve hareket olmadiginda sure isler.

### Akilli Izleyici
1. **"Akilli Izleyici"** sekmesine tiklayin
2. **Islem Izle** veya **Ag Izle** secenegini belirleyin
3. Ilgili program adini veya hiz sinirini (KB/s) girip **"BASLAT"** butonuna tiklayin.

---

## Kod Yapisi ve Calisma Mantigi (Zamanlayici.cs)

Uygulama tek bir C# dosyasi (`Zamanlayici.cs`) icerisinde moduler siniflara ayrilarak gelistirilmistir. Kodun temel isleyisi asagidaki bilesenlerle saglanir:

### 1. Kullanici Arayuzu (UI) Bilesenleri
- **`BufferedPanel` Sinifi:** Standart `Panel` kontrolunu genisleterek **Double-Buffering** (Cift arabellege alma) ozelligini aktif eder. Bu sayede animasyonlu ilerleme halkasi saniyede bir cizilirken ekranda titreme (flicker) olusmasi engellenir.
- **`RoundedButton` Sinifi:** Standart Windows butonlarini modern bir gorunume kavusturmak icin tasarlanmistir. Yuvarlatilmis koseler (`Radius`), gradient renk gecisleri, hover (uzerine gelme) efektleri ve saydamlik ayarlari `OnPaint` metodu ezilerek (override) GDI+ ile ozel olarak cizdirilir (Owner-drawn).

### 2. Form ve Kontrol Sinifi (`MainForm`)
- Uygulamanin kalbidir. Arayuz elemanlarinin (butonlar, gostergeler) dinamik olarak olusturulmasi (kod uzerinden UI insasi), renk paletinin tanimlanmasi (Orn: `BgDark`, `AccentBlue`) ve click gibi olaylarin (event) dinlenmesi burada yapilir.
- **Gorsel Cizimler:** `PaintTimerRing` metodu, geri sayim devam ederken gecen sureye orantili olarak (`timerProgress`) parlak bir ilerleme halkasi ve uc noktasi (Glow effect) cizer.

### 3. Zamanlayici ve Izleme Mekanizmalari
- **Standart Geri Sayim:** `System.Windows.Forms.Timer` kullanilarak saniyede bir tetiklenen (`CountdownTick`) bir dongu ile sure azaltilir.
- **Hareketsizlik Izleme (Idle Detection):** `[DllImport("user32.dll")]` kullanilarak Windows API'sinin `GetLastInputInfo` fonksiyonu disaridan (extern) cagirilir. Bu fonksiyon, isletim sistemi seviyesinde en son ne zaman klavye veya fare girisi yapildigini milisaniye cinsinden dondurur. Bu deger surekli kontrol edilerek hedeflenen bosta kalma suresine ulasilip ulasilmadigi hesaplanir.
- **Akilli Izleyici (Ag ve Surec):**
  - *Islem Izleme:* `Process.GetProcessesByName()` ile sistemdeki aktif islemler taranir. Hedef program listede bulunamazsa kapanma islemi gerceklesir.
  - *Ag Izleme:* `System.Net.NetworkInformation.NetworkInterface` sinifi kullanilarak tum aktif ag bagdastiricilarinin `BytesReceived` (alinan byte) istatistikleri toplanir. Saniyede alinan veri miktari hesaplanip KB/s cinsine cevrilerek, kullanicinin belirledigi hiz sinirinin altina dusup dusmedigi denetlenir.

### 4. Isletim Sistemi Komutlari
- Hedeflenen sure veya kosul saglandiginda, `Process.Start` ile Windows'un yerlesik araci uzerinden `shutdown /s /t 30` (Kapat) veya `shutdown /r /t 30` (Yeniden Baslat) komutlari arka planda calistirilir. Kullaniciya islem icin 30 saniyelik ek bir uyari payi birakilir.

---

## Teknik Detaylar

- **Dil:** C# (.NET Framework 4.0+)
- **UI Framework:** Windows Forms (WinForms)
- **Idle Detection:** `user32.dll` - `GetLastInputInfo` API
- **Ag Izleme:** `System.Net.NetworkInformation.NetworkInterface` sinifi ile canli veri akisi olcumu
- **Shutdown:** Windows `shutdown.exe` komutu
- **Uyumluluk:** Windows 7, 8, 10, 11

---

## Lisans

Bu proje MIT Lisansi altinda lisanslanmistir.
