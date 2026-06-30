# Zamanlayici - PC Kapatma & Yeniden Baslatma

Windows icin gelistirilmis, modern arayuzlu PC kapatma zamanlayici uygulamasi.

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
Mouse ve klavye hareketlerini izler. Belirlediginiz sure boyunca hic hareket olmazsa bilgisayari kapatir.
- Windows API `GetLastInputInfo` ile sistem bosta kalma suresi izlenir
- Herhangi bir mouse/klavye hareketi sayaci otomatik sifirlar
- Canli bosta kalma suresi gosterimi

### Genel
- **Kapat** veya **Yeniden Baslat** secimi
- **Modern koyu tema** arayuz
- **Owner-drawn yuvarlatilmis butonlar** ve gradient efektler
- **Animasyonlu ilerleme halkasi** (parlayan nokta efekti)
- **Iptal** butonu ile geri sayim veya izlemeyi durdurma
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
5. Onay verdikten sonra geri sayim baslar
6. Iptal etmek icin **"IPTAL ET"** butonuna tiklayin

### Hareketsizlik Modu
1. **"Hareketsizlik"** sekmesine tiklayin
2. Bosta kalma esik suresini belirleyin (orn: 30 dakika)
3. **"IZLEMEYI BASLAT"** butonuna tiklayin
4. Uygulama mouse/klavye hareketlerini izlemeye baslar
5. Eger belirlenen sure boyunca hic hareket olmazsa PC kapanir
6. Herhangi bir hareket sayaci otomatik sifirlar

---

## Teknik Detaylar

- **Dil:** C# (.NET Framework 4.0+)
- **UI Framework:** Windows Forms (WinForms)
- **Idle Detection:** `user32.dll` - `GetLastInputInfo` API
- **Shutdown:** Windows `shutdown.exe` komutu
- **Uyumluluk:** Windows 7, 8, 10, 11

---

## Lisans

Bu proje MIT Lisansi altinda lisanslanmistir.
