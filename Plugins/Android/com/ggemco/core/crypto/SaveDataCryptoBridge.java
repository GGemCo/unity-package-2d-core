package com.ggemco.core.crypto;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.SecureRandom;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Android Keystore를 사용해 Unity 저장 데이터를 AES-GCM으로 암호화하고 복호화합니다.
 */
public final class SaveDataCryptoBridge {
    private static final String ANDROID_KEYSTORE = "AndroidKeyStore";
    private static final String TRANSFORMATION = "AES/GCM/NoPadding";
    private static final String MAGIC = "GGEM_SAVE";
    private static final String ALGORITHM = "AES-256-GCM";
    private static final String DEFAULT_KEY_ALIAS = "ggemco_save_key_v1";
    private static final int VERSION = 2;
    private static final int KEY_SIZE_BITS = 256;
    private static final int GCM_TAG_BITS = 128;
    private static final int NONCE_SIZE_BYTES = 12;

    private SaveDataCryptoBridge() {
    }

    /**
     * 평문 저장 데이터를 암호화 Envelope 문자열로 변환합니다.
     *
     * @param plainText 저장할 평문 JSON 문자열입니다.
     * @param keyAlias Android Keystore에서 사용할 키 별칭입니다.
     * @param associatedData 암호문 검증에 사용할 추가 인증 데이터입니다.
     * @return 파일에 기록할 암호화 Envelope JSON 문자열입니다.
     * @throws Exception 키 생성 또는 암호화에 실패하면 발생합니다.
     */
    public static String encrypt(String plainText, String keyAlias, String associatedData) throws Exception {
        String normalizedKeyAlias = normalizeKeyAlias(keyAlias);
        SecretKey key = getOrCreateSecretKey(normalizedKeyAlias);
        byte[] nonce = new byte[NONCE_SIZE_BYTES];
        new SecureRandom().nextBytes(nonce);

        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.ENCRYPT_MODE, key, new GCMParameterSpec(GCM_TAG_BITS, nonce));
        applyAssociatedData(cipher, associatedData);

        byte[] encryptedBytes = cipher.doFinal(nullToEmpty(plainText).getBytes(StandardCharsets.UTF_8));

        JSONObject envelope = new JSONObject();
        envelope.put("magic", MAGIC);
        envelope.put("version", VERSION);
        envelope.put("algorithm", ALGORITHM);
        envelope.put("keyAlias", normalizedKeyAlias);
        envelope.put("tagBits", GCM_TAG_BITS);
        envelope.put("nonce", Base64.encodeToString(nonce, Base64.NO_WRAP));
        envelope.put("payload", Base64.encodeToString(encryptedBytes, Base64.NO_WRAP));
        return envelope.toString();
    }

    /**
     * 암호화 Envelope 문자열을 평문 저장 데이터로 복호화합니다.
     *
     * @param envelopeText 파일에서 읽은 암호화 Envelope JSON 문자열입니다.
     * @param keyAlias Android Keystore에서 사용할 키 별칭입니다.
     * @param associatedData 암호문 검증에 사용할 추가 인증 데이터입니다.
     * @return 역직렬화에 사용할 평문 JSON 문자열입니다.
     * @throws Exception 키 조회 또는 복호화에 실패하면 발생합니다.
     */
    public static String decrypt(String envelopeText, String keyAlias, String associatedData) throws Exception {
        JSONObject envelope = new JSONObject(envelopeText);
        if (!MAGIC.equals(envelope.optString("magic"))) {
            throw new IllegalArgumentException("저장 데이터 암호화 Envelope 형식이 아닙니다.");
        }

        byte[] nonce = Base64.decode(envelope.getString("nonce"), Base64.NO_WRAP);
        byte[] encryptedBytes = Base64.decode(envelope.getString("payload"), Base64.NO_WRAP);

        String envelopeKeyAlias = normalizeKeyAlias(envelope.optString("keyAlias", keyAlias));
        SecretKey key = getOrCreateSecretKey(envelopeKeyAlias);
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(envelope.optInt("tagBits", GCM_TAG_BITS), nonce));
        applyAssociatedData(cipher, associatedData);

        byte[] plainBytes = cipher.doFinal(encryptedBytes);
        return new String(plainBytes, StandardCharsets.UTF_8);
    }

    /**
     * Android Keystore에 저장된 AES 키를 가져오거나 새로 생성합니다.
     *
     * @param keyAlias 키 별칭입니다.
     * @return 암호화와 복호화에 사용할 AES 키입니다.
     * @throws Exception 키 저장소 접근 또는 키 생성에 실패하면 발생합니다.
     */
    private static SecretKey getOrCreateSecretKey(String keyAlias) throws Exception {
        KeyStore keyStore = KeyStore.getInstance(ANDROID_KEYSTORE);
        keyStore.load(null);

        if (keyStore.containsAlias(keyAlias)) {
            KeyStore.Entry entry = keyStore.getEntry(keyAlias, null);
            if (entry instanceof KeyStore.SecretKeyEntry) {
                return ((KeyStore.SecretKeyEntry) entry).getSecretKey();
            }
        }

        KeyGenerator keyGenerator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEYSTORE);
        KeyGenParameterSpec keySpec = new KeyGenParameterSpec.Builder(
                keyAlias,
                KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(KEY_SIZE_BITS)
                .setRandomizedEncryptionRequired(true)
                .build();

        keyGenerator.init(keySpec);
        return keyGenerator.generateKey();
    }

    /**
     * AES-GCM 추가 인증 데이터를 암호화 객체에 적용합니다.
     *
     * @param cipher 암호화 또는 복호화에 사용할 Cipher입니다.
     * @param associatedData 추가 인증 데이터입니다.
     */
    private static void applyAssociatedData(Cipher cipher, String associatedData) {
        if (associatedData == null || associatedData.length() == 0) {
            return;
        }

        cipher.updateAAD(associatedData.getBytes(StandardCharsets.UTF_8));
    }

    /**
     * 비어 있는 키 별칭을 기본 별칭으로 치환합니다.
     *
     * @param keyAlias 외부에서 전달된 키 별칭입니다.
     * @return 실제로 사용할 키 별칭입니다.
     */
    private static String normalizeKeyAlias(String keyAlias) {
        if (keyAlias == null || keyAlias.trim().length() == 0) {
            return DEFAULT_KEY_ALIAS;
        }

        return keyAlias.trim();
    }

    /**
     * null 문자열을 빈 문자열로 변환합니다.
     *
     * @param value 변환할 문자열입니다.
     * @return null이 아닌 문자열입니다.
     */
    private static String nullToEmpty(String value) {
        return value == null ? "" : value;
    }
}
