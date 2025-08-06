const localVideo = document.getElementById('localVideo');
const remoteVideo = document.getElementById('remoteVideo');
const startCallBtn = document.getElementById('startCallBtn');
const endCallBtn = document.getElementById('endCallBtn');
const chatInput = document.getElementById('chatInput');
const sendBtn = document.getElementById('sendBtn');
const chatMessages = document.getElementById('chatMessages');

let localStream;
let peerConnection;
let connection;
let targetUser = ''; // Will hold the peer email/userId

const config = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        {
            urls: 'turn:openrelay.metered.ca:443',
            username: 'openrelayproject',
            credential: 'openrelayproject'
        }
    ]
};

async function initSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .build();

    connection.on("ReceiveOffer", async (fromUser, offer) => {
        console.log("📩 Received offer from:", fromUser);
        targetUser = fromUser;
        await createPeerConnection();
        await peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(offer)));

        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        await connection.invoke("SendAnswer", targetUser, JSON.stringify(answer));
        console.log("📤 Sent answer to:", targetUser);
    });

    connection.on("ReceiveAnswer", async (fromUser, answer) => {
        console.log("📩 Received answer from:", fromUser);
        await peerConnection.setRemoteDescription(new RTCSessionDescription(JSON.parse(answer)));
    });

    connection.on("ReceiveIceCandidate", async (fromUser, candidate) => {
        console.log("❄️ Received ICE candidate from:", fromUser);
        if (candidate) {
            await peerConnection.addIceCandidate(new RTCIceCandidate(JSON.parse(candidate)));
        }
    });

    connection.on("ReceiveChatMessage", (sender, message) => {
        const msg = `<div><strong>${sender}:</strong> ${message}</div>`;
        chatMessages.innerHTML += msg;
        chatMessages.scrollTop = chatMessages.scrollHeight;
    });

    await connection.start();
    console.log("✅ SignalR connected");

    const currentUser = document.querySelector("body").innerText.match(/Hello,\s+(.+?)!/);
    if (currentUser && currentUser[1]) {
        await connection.invoke("RegisterUser", currentUser[1].trim());
        console.log("🔐 Registered user:", currentUser[1].trim());
    } else {
        console.warn("⚠️ Could not determine current user identity.");
    }

    // ✅ Attach media stream immediately for both caller and receiver
    try {
        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        localVideo.srcObject = localStream;
        console.log("📷 Local stream initialized");
    } catch (err) {
        console.error("🚫 Error accessing media devices:", err);
        alert("Camera or microphone access was denied or not available.");
    }
}

async function startCall() {
    targetUser = prompt("Enter recipient email/username:").toLowerCase().trim();
    if (!targetUser) return;
    console.log("📞 Starting call to:", targetUser);

    await createPeerConnection();

    // Add local stream to peer connection
    if (localStream) {
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));
    }

    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    await connection.invoke("SendOffer", targetUser, JSON.stringify(offer));
    console.log("📤 Sent offer to:", targetUser);
}

async function createPeerConnection() {
    peerConnection = new RTCPeerConnection(config);

    peerConnection.onicecandidate = event => {
        if (event.candidate) {
            connection.invoke("SendIceCandidate", targetUser, JSON.stringify(event.candidate));
        }
    };

    peerConnection.ontrack = event => {
        console.log("🎥 Remote stream received");
        remoteVideo.srcObject = event.streams[0];
    };

    // Optional: log connection state
    peerConnection.onconnectionstatechange = () => {
        console.log("🔗 Connection state:", peerConnection.connectionState);
    };
}

function endCall() {
    if (peerConnection) {
        peerConnection.close();
        peerConnection = null;
    }
    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }
    remoteVideo.srcObject = null;
    localVideo.srcObject = null;
    console.log("📴 Call ended");
}

sendBtn.addEventListener('click', async () => {
    const message = chatInput.value.trim();
    if (!message || !targetUser) return;

    await connection.invoke("SendChatMessage", targetUser, message);
    const msg = `<div><strong>You:</strong> ${message}</div>`;
    chatMessages.innerHTML += msg;
    chatInput.value = '';
    chatMessages.scrollTop = chatMessages.scrollHeight;
});

startCallBtn.addEventListener('click', startCall);
endCallBtn.addEventListener('click', endCall);

initSignalR();
