// firebase.js

// Assurez-vous que Firebase est bien initialisé
const firebaseConfig = {
    apiKey: "AIzaSyALezAiO6-OxSofqJ_yvCBaRwi00bdszXo",
    authDomain: "sample-firebase-ai-app-866bf.firebaseapp.com",
    projectId: "sample-firebase-ai-app-866bf",
    storageBucket: "sample-firebase-ai-app-866bf.firebasestorage.app",
    messagingSenderId: "135560051935",
    appId: "1:135560051935:web:c23bcc21326f0c3db93d6a"
};

firebase.initializeApp(firebaseConfig);
const db = firebase.firestore();

// Activer la persistance avant toute opération
db.enablePersistence()
    .catch((err) => {
        if (err.code == 'failed-precondition') {
            console.log("La persistance a échoué. Plusieurs onglets ouverts ?");
        } else if (err.code == 'unimplemented') {
            console.log("La persistance n'est pas supportée dans ce navigateur.");
        }
    });
