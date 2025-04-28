let video;
let canvas;
let detector;

// Modelleri yükle
async function modelleriYukle() {
   //// ...existing code...

async function init() {
    video = document.getElementById('videoElement');
    canvas = document.getElementById('overlay');
    
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        video.srcObject = stream;
    } catch (err) {
        console.error('Webcam erişimi hatası:', err);
    }

    await modelleriYukle();
    console.log('Modeller yüklendi');

    // Canvas boyutlarını video boyutlarıyla eşleştir
    canvas.width = video.width;
    canvas.height = video.height;

    // Sürekli yüz tespiti yap
    setInterval(async () => {
        const detections = await faceapi.detectAllFaces(video)
            .withFaceLandmarks();

        // Canvas'ı temizle
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Tespit edilen yüzleri çiz
        if (detections && detections.length > 0) {
            detections.forEach(detection => {
                const box = detection.detection.box;
                ctx.strokeStyle = 'blue';
                ctx.lineWidth = 3;
                ctx.strokeRect(box.x, box.y, box.width, box.height);
            });
        }
    }, 100); // Her 100ms'de bir güncelle
}

// ...existing code... const modelPath = 'https://justadudewhohacks.github.io/face-api.js/models/';
    const modelPath='/js/faceApi/models/';
    await faceapi.nets.ssdMobilenetv1.loadFromUri(modelPath);
    await faceapi.nets.faceLandmark68Net.loadFromUri(modelPath);
    await faceapi.nets.faceRecognitionNet.loadFromUri(modelPath);
}

// Webcam'i başlat
// ...existing code...

async function init() {
    video = document.getElementById('videoElement');
    canvas = document.getElementById('overlay');
    
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        video.srcObject = stream;
    } catch (err) {
        console.error('Webcam erişimi hatası:', err);
    }

    await modelleriYukle();
    console.log('Modeller yüklendi');

    // Canvas boyutlarını video boyutlarıyla eşleştir
    canvas.width = video.width;
    canvas.height = video.height;

    // Sürekli yüz tespiti yap
    setInterval(async () => {
        const detections = await faceapi.detectAllFaces(video)
            .withFaceLandmarks();

        // Canvas'ı temizle
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Tespit edilen yüzleri çiz
        if (detections && detections.length > 0) {
            detections.forEach(detection => {
                const box = detection.detection.box;
                ctx.strokeStyle = 'blue';
                ctx.lineWidth = 3;
                ctx.strokeRect(box.x, box.y, box.width, box.height);
            });
        }
    }, 200); // Her 100ms'de bir güncelle
}

// ...existing code...

// Yüz bilgilerini kaydet
async function yuzKaydet() {
    const isim = document.getElementById('isim').value;
    
    if (!isim) {
        alert('Lütfen bir isim giriniz!');
        return;
    }

    const detections = await faceapi.detectSingleFace(video)
        .withFaceLandmarks()
        .withFaceDescriptor();

    if (!detections) {
        alert('Yüz tespit edilemedi! Lütfen kameraya bakın.');
        return;
    }

    const yuzBilgisi = {
        name: isim,
        descriptor: Array.from(detections.descriptor).join(',')
    };

    try {
        const response = await fetch('/Face/SaveFace', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(yuzBilgisi)
        });

        if (!response.ok) {
            throw new Error('Yüz kaydedilirken bir hata oluştu');
        }

        const result = await response.json();
        alert(result.message);
    } catch (error) {
        console.error('Yüz kaydetme hatası:', error);
        alert('Yüz kaydedilirken bir hata oluştu');
    }
}

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', init); 