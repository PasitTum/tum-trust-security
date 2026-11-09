package com.tum.trust.security.hil;

import javacard.framework.APDU;
import javacard.framework.Applet;
import javacard.framework.ISO7816;
import javacard.framework.ISOException;
import javacard.framework.Util;

/**
 * TUM Trust Security — HIL (Hardware-In-The-Loop) APDU Emulator Applet
 * Targets NXP JCOP (J3H081 / J3R180 / JCOP4) Smart Cards (Java Card 3.0.4+ / GlobalPlatform 2.2+).
 * 
 * Emulates dual personality:
 * 1. Thai National ID Card (MoI CLA=0x80 INS=0xB0)
 * 2. ICAO Doc 9303 eMRTD ePassport (CLA=0x00 INS=0xB0)
 * 
 * Allows kiosk hardware testing with 100% realistic ISO 7816 / 14443 APDU timing
 * without requiring real citizen credentials.
 */
public class ThaiIdAndPassportApplet extends Applet {

    // Applet AID definitions
    public static final byte[] AID_THAI_ID = {
        (byte)0xA0, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x54, (byte)0x48, (byte)0x00, (byte)0x01
    };
    public static final byte[] AID_ICAO_PASSPORT = {
        (byte)0xA0, (byte)0x00, (byte)0x00, (byte)0x02, (byte)0x47, (byte)0x10, (byte)0x01
    };

    // Instruction bytes
    private static final byte INS_SELECT = (byte)0xA4;
    private static final byte INS_READ_BINARY = (byte)0xB0;
    private static final byte INS_GET_CHALLENGE = (byte)0x84;
    private static final byte INS_INTERNAL_AUTH = (byte)0x88;

    // Specimen Mock Datasets
    private static final byte[] SPECIMEN_CID = {
        '1', '1', '0', '0', '5', '0', '0', '1', '2', '3', '4', '5', '1'
    };
    private static final byte[] SPECIMEN_NAME_TH_EN = {
        (byte)0xE0, (byte)0xB8, (byte)0x99, (byte)0xE0, (byte)0xB8, (byte)0xB2, (byte)0xE0, (byte)0xB8, (byte)0xA2, // นาย
        '#',
        (byte)0xE0, (byte)0xB8, (byte)0xAA, (byte)0xE0, (byte)0xB8, (byte)0xA1, (byte)0xE0, (byte)0xB8, (byte)0x8A, (byte)0xE0, (byte)0xB8, (byte)0xB2, (byte)0xE0, (byte)0xB8, (byte)0xA2, // สมชาย
        '#', '#',
        (byte)0xE0, (byte)0xB8, (byte)0x8improved, (byte)0xE0, (byte)0xB8, (byte)0x88, (byte)0xE0, (byte)0xB8, (byte)0xB1, (byte)0xE0, (byte)0xB8, (byte)0x94, // ใจดี
        '#',
        'M', 'r', '.', '#', 'S', 'o', 'm', 'c', 'h', 'a', 'i', '#', '#', 'J', 'a', 'i', 'd', 'e', 'e'
    };
    private static final byte[] SPECIMEN_DOB = { '2', '5', '3', '5', '0', '5', '1', '2' };
    private static final byte[] SPECIMEN_GENDER = { '1' };
    private static final byte[] SPECIMEN_ISSUE = { '2', '5', '6', '5', '0', '1', '1', '5' };
    private static final byte[] SPECIMEN_EXPIRE = { '2', '5', '7', '3', '0', '5', '1', '2' };

    // ICAO TD3 Specimen MRZ
    private static final byte[] SPECIMEN_MRZ = {
        'P', '<', 'U', 'T', 'O', 'E', 'R', 'I', 'K', 'S', 'S', 'O', 'N', '<', '<', 
        'A', 'N', 'N', 'A', '<', 'M', 'A', 'R', 'I', 'A', '<', '<', '<', '<', '<', 
        '<', '<', '<', '<', '<', '<', '<', '<', '<', '<', '<', '<', '<', '<',
        'L', '8', '9', '8', '9', '0', '2', 'C', '3', '6', 'U', 'T', 'O', '7', '4', 
        '0', '8', '1', '2', '2', 'F', '1', '2', '0', '4', '1', '5', '9', 'Z', 'E', 
        '1', '8', '4', '2', '2', '6', 'B', '<', '<', '<', '<', '<', '1', '0'
    };

    // Minimal JPEG header for facial image tests
    private static final byte[] JPEG_HEADER = {
        (byte)0xFF, (byte)0xD8, (byte)0xFF, (byte)0xE0, (byte)0x00, (byte)0x10,
        'J', 'F', 'I', 'F', (byte)0x00, (byte)0x01, (byte)0x01, (byte)0x01,
        (byte)0x00, (byte)0x48, (byte)0x00, (byte)0x48, (byte)0x00, (byte)0x00,
        (byte)0xFF, (byte)0xDB, (byte)0x00, (byte)0x43, (byte)0x00,
        (byte)0x08, (byte)0x06, (byte)0x06, (byte)0x07, (byte)0x06, (byte)0x05,
        (byte)0xFF, (byte)0xD9
    };

    protected ThaiIdAndPassportApplet() {
        register();
    }

    public static void install(byte[] bArray, short bOffset, byte bLength) {
        new ThaiIdAndPassportApplet();
    }

    public void process(APDU apdu) {
        if (selectingApplet()) {
            return;
        }

        byte[] buffer = apdu.getBuffer();
        byte cla = buffer[ISO7816.OFFSET_CLA];
        byte ins = buffer[ISO7816.OFFSET_INS];
        byte p1 = buffer[ISO7816.OFFSET_P1];
        byte p2 = buffer[ISO7816.OFFSET_P2];

        // 1. SELECT command (0x00 0xA4)
        if (cla == 0x00 && ins == INS_SELECT) {
            return; // 90 00 Success
        }

        // 2. Thai National ID Read Binary (0x80 0xB0)
        if (cla == (byte)0x80 && ins == INS_READ_BINARY) {
            handleThaiIdRead(apdu, buffer, p1, p2);
            return;
        }

        // 3. ICAO Doc 9303 Read Binary (0x00 0xB0)
        if (cla == 0x00 && ins == INS_READ_BINARY) {
            handleIcaoRead(apdu, buffer, p1, p2);
            return;
        }

        // 4. GET CHALLENGE (0x00 0x84) for Active Authentication
        if (cla == 0x00 && ins == INS_GET_CHALLENGE) {
            byte[] nonce = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            Util.arrayCopyNonReentrant(nonce, (short)0, buffer, (short)0, (short)nonce.length);
            apdu.setOutgoingAndSend((short)0, (short)nonce.length);
            return;
        }

        // Unknown APDU
        ISOException.throwIt(ISO7816.SW_INS_NOT_SUPPORTED);
    }

    private void handleThaiIdRead(APDU apdu, byte[] buffer, byte p1, byte p2) {
        // P1=0x00, P2=0x04 -> Citizen ID (13 bytes)
        if (p1 == 0x00 && p2 == 0x04) {
            sendResponse(apdu, buffer, SPECIMEN_CID);
            return;
        }

        // P1=0x00, P2=0x11 -> Thai & English Name
        if (p1 == 0x00 && p2 == 0x11) {
            sendResponse(apdu, buffer, SPECIMEN_NAME_TH_EN);
            return;
        }

        // P1=0x00, P2=0xD1 -> Date of Birth
        if (p1 == 0x00 && p2 == (byte)0xD1) {
            sendResponse(apdu, buffer, SPECIMEN_DOB);
            return;
        }

        // P1=0x00, P2=0xE1 -> Gender
        if (p1 == 0x00 && p2 == (byte)0xE1) {
            sendResponse(apdu, buffer, SPECIMEN_GENDER);
            return;
        }

        // P1=0x01, P2=0x67 -> Issue Date
        if (p1 == 0x01 && p2 == 0x67) {
            sendResponse(apdu, buffer, SPECIMEN_ISSUE);
            return;
        }

        // P1=0x01, P2=0x6F -> Expiry Date
        if (p1 == 0x01 && p2 == 0x6F) {
            sendResponse(apdu, buffer, SPECIMEN_EXPIRE);
            return;
        }

        // P1=0x01, P2=0x7B -> Photo Segment
        if (p1 == 0x01 && p2 == 0x7B) {
            sendResponse(apdu, buffer, JPEG_HEADER);
            return;
        }

        // Fallback Success
        ISOException.throwIt(ISO7816.SW_NO_ERROR);
    }

    private void handleIcaoRead(APDU apdu, byte[] buffer, byte p1, byte p2) {
        // Return simulated ICAO MRZ or LDS Data Group payload
        sendResponse(apdu, buffer, SPECIMEN_MRZ);
    }

    private void sendResponse(APDU apdu, byte[] buffer, byte[] data) {
        Util.arrayCopyNonReentrant(data, (short)0, buffer, (short)0, (short)data.length);
        apdu.setOutgoingAndSend((short)0, (short)data.length);
    }
}
