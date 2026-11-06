-- =========================================================================================
-- KIM LONG ECOSTAY - CRITICAL SECURITY HARDENING & SMARTLOCK PIN ROTATION MIGRATION
-- Mục tiêu:
-- 1. Thu hồi toàn bộ mã PIN SmartLock cũ từng bị lộ (839201, 512940, 724819, v.v.).
-- 2. Bổ sung cơ chế chống vét cạn (Brute-force Lockout: FailedAttempts, LockedUntil) và Salt cho SmartLockPins.
-- 3. Xoay vòng (Rotate) sang mã băm SHA-256 ngẫu nhiên an toàn mật mã (CSPRNG), đánh dấu dữ liệu mô phỏng [DEMO-SIMULATION].
-- 4. Nâng cấp bảng AppUsers sang lưu trữ băm mật khẩu một chiều Salted SHA-256 (Không lưu mật khẩu dạng rõ).
-- =========================================================================================

SET NOCOUNT ON;
GO

-- 1. BỔ SUNG CỘT BẢO MẬT BRUTE-FORCE & SALT CHO BẢNG SmartLockPins
IF COL_LENGTH('dbo.SmartLockPins', 'PinSalt') IS NULL
BEGIN
    ALTER TABLE dbo.SmartLockPins ADD PinSalt VARCHAR(64) NOT NULL CONSTRAINT DF_SmartLockPins_PinSalt DEFAULT ('KLE_SL_SALT_2026');
END
GO

IF COL_LENGTH('dbo.SmartLockPins', 'FailedAttempts') IS NULL
BEGIN
    ALTER TABLE dbo.SmartLockPins ADD FailedAttempts INT NOT NULL CONSTRAINT DF_SmartLockPins_FailedAttempts DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.SmartLockPins', 'LockedUntil') IS NULL
BEGIN
    ALTER TABLE dbo.SmartLockPins ADD LockedUntil DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.SmartLockPins', 'IsDemoSimulation') IS NULL
BEGIN
    ALTER TABLE dbo.SmartLockPins ADD IsDemoSimulation BIT NOT NULL CONSTRAINT DF_SmartLockPins_IsDemo DEFAULT (1);
END
GO

-- 2. THU HỒI TOÀN BỘ PIN CŨ BỊ LỘ & XOAY VÒNG SANG HASH MỚI AN TOÀN MẬT MÃ
-- Phòng KLE-R101 đang gắn với đơn chờ đối soát cọc (PENDING_DEPOSIT) -> Thu hồi về STANDBY (Chưa kích hoạt PIN khi chưa cọc)
UPDATE dbo.SmartLockPins
SET PinSalt = CONVERT(VARCHAR(64), NEWID()),
    PinHash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(CONCAT('REVOKED:KLE-R101:', NEWID(), ':', CRYPT_GEN_RANDOM(16)) AS VARCHAR(256))), 2),
    MaskedDisplay = '••••••',
    PinStatus = 'STANDBY',
    FailedAttempts = 0,
    LockedUntil = NULL,
    IsDemoSimulation = 1,
    LastReissuedBy = N'Security Engine (Auto-Revoke)',
    LastReissuedReason = N'[DEMO-SIMULATION] Thu hồi PIN mặc định cũ; đơn đang chờ đối soát cọc 50%',
    UpdatedAt = SYSUTCDATETIME(),
    SyncVersion = SyncVersion + 1
WHERE RoomNumber = 'KLE-R101';

-- Phòng KLE-R102 & KLE-R201 (Đơn hợp lệ CONFIRMED / CHECKED_IN) -> Thu hồi PIN cũ, xoay vòng sang CSPRNG SHA-256 mới
UPDATE dbo.SmartLockPins
SET PinSalt = CONVERT(VARCHAR(64), NEWID()),
    PinHash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(CONCAT('ROTATED:', RoomNumber, ':', NEWID(), ':', CRYPT_GEN_RANDOM(16)) AS VARCHAR(256))), 2),
    MaskedDisplay = '••••••',
    PinStatus = 'ACTIVE',
    ValidFrom = DATEADD(HOUR, -12, SYSUTCDATETIME()),
    ValidUntil = DATEADD(DAY, 3, SYSUTCDATETIME()),
    FailedAttempts = 0,
    LockedUntil = NULL,
    IsDemoSimulation = 1,
    LastReissuedBy = N'Security Engine (CSPRNG Rotation)',
    LastReissuedReason = N'[DEMO-SIMULATION] Thu hồi PIN cũ bị lộ và cấp lại mã khóa CSPRNG SHA-256 bảo mật',
    UpdatedAt = SYSUTCDATETIME(),
    SyncVersion = SyncVersion + 1
WHERE RoomNumber IN ('KLE-R102', 'KLE-R201');

-- Phòng KLE-R202 (STANDBY) & KLE-R203 (EXPIRED) -> Xóa bỏ mọi hash cũ
UPDATE dbo.SmartLockPins
SET PinSalt = CONVERT(VARCHAR(64), NEWID()),
    PinHash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(CONCAT('STANDBY:', RoomNumber, ':', NEWID(), ':', CRYPT_GEN_RANDOM(16)) AS VARCHAR(256))), 2),
    MaskedDisplay = '••••••',
    FailedAttempts = 0,
    LockedUntil = NULL,
    IsDemoSimulation = 1,
    LastReissuedBy = N'Security Engine',
    LastReissuedReason = N'[DEMO-SIMULATION] Đã thu hồi toàn bộ mã PIN mặc định cũ',
    UpdatedAt = SYSUTCDATETIME(),
    SyncVersion = SyncVersion + 1
WHERE RoomNumber IN ('KLE-R202', 'KLE-R203');
GO

-- 3. GHI NHẬN LỊCH SỬ THU HỒI & CẤP LẠI TRONG SmartLockAuditLogs (TUYỆT ĐỐI KHÔNG GHI GIÁ TRỊ PIN)
DELETE FROM dbo.SmartLockAuditLogs WHERE Reason LIKE '%839201%' OR Reason LIKE '%512940%' OR Reason LIKE '%724819%';

INSERT INTO dbo.SmartLockAuditLogs (LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason, CreatedAt)
VALUES
    (CONCAT('SL-SEC-REV-', FORMAT(SYSUTCDATETIME(), 'HHmmss'), '-01'), 'KLE-R101', 'KLE-20261018-01', 'REVOKE_EXPOSED_PIN', N'Security Remediation Engine', 'SYSTEM_SECURITY', N'[DEMO-SIMULATION] Thu hồi PIN mặc định cũ; chuyển khóa về STANDBY chờ xác nhận cọc 50%', SYSUTCDATETIME()),
    (CONCAT('SL-SEC-ROT-', FORMAT(SYSUTCDATETIME(), 'HHmmss'), '-02'), 'KLE-R102', 'KLE-20261015-88', 'ROTATE_CSPRNG_PIN', N'Security Remediation Engine', 'SYSTEM_SECURITY', N'[DEMO-SIMULATION] Thu hồi PIN cũ và xoay vòng mã khóa CSPRNG SHA-256 mới (Không lưu PIN rõ)', SYSUTCDATETIME()),
    (CONCAT('SL-SEC-ROT-', FORMAT(SYSUTCDATETIME(), 'HHmmss'), '-03'), 'KLE-R201', 'KLE-20261014-42', 'ROTATE_CSPRNG_PIN', N'Security Remediation Engine', 'SYSTEM_SECURITY', N'[DEMO-SIMULATION] Thu hồi PIN cũ và xoay vòng mã khóa CSPRNG SHA-256 mới (Không lưu PIN rõ)', SYSUTCDATETIME());
GO

-- 4. ĐÁNH DẤU RÕ DỮ LIỆU BOOKING MẪU LÀ [DEMO-SIMULATION]
UPDATE dbo.Bookings
SET Notes = CASE 
    WHEN Notes IS NULL OR Notes = '' THEN N'[DEMO-SIMULATION] Dữ liệu mô phỏng kiểm thử'
    WHEN Notes NOT LIKE '%[DEMO-SIMULATION]%' THEN CONCAT(N'[DEMO-SIMULATION] ', Notes)
    ELSE Notes
END
WHERE BookingCode IN ('BK20261015-01', 'KLE-20261018-01', 'KLE-20261015-88', 'KLE-20261014-42', 'KLE-20261010-19');
GO

-- 5. CẬP NHẬT STORED PROCEDURE sp_ReissueSmartLockPinWithAudit ĐỂ HỖ TRỢ SALT & RESET BRUTE-FORCE LOCKOUT
CREATE OR ALTER PROCEDURE dbo.sp_ReissueSmartLockPinWithAudit
    @RoomNumber VARCHAR(20),
    @BookingCode VARCHAR(30) = NULL,
    @NewPinHash VARCHAR(128),
    @PinSalt VARCHAR(64) = 'KLE_SL_SALT_2026',
    @PerformedBy NVARCHAR(100),
    @RoleName VARCHAR(50),
    @Reason NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SmartLockPins
        SET PinHash = @NewPinHash,
            PinSalt = @PinSalt,
            MaskedDisplay = '••••••',
            PinStatus = 'ACTIVE',
            FailedAttempts = 0,
            LockedUntil = NULL,
            ValidFrom = ISNULL(ValidFrom, DATEADD(HOUR, -1, SYSUTCDATETIME())),
            ValidUntil = ISNULL(ValidUntil, DATEADD(DAY, 2, SYSUTCDATETIME())),
            LastReissuedBy = @PerformedBy,
            LastReissuedReason = @Reason,
            UpdatedAt = SYSUTCDATETIME(),
            SyncVersion = SyncVersion + 1
        WHERE RoomNumber = @RoomNumber;

        DECLARE @LogCode VARCHAR(40) = CONCAT('SL-AUDIT-', FORMAT(SYSUTCDATETIME(), 'yyyyMMddHHmmss'), '-', RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS VARCHAR(10)), 3));

        INSERT INTO dbo.SmartLockAuditLogs (
            LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason, CreatedAt
        )
        VALUES (
            @LogCode, @RoomNumber, @BookingCode, 'REISSUE_PIN', @PerformedBy, @RoleName, @Reason, SYSUTCDATETIME()
        );

        INSERT INTO dbo.SyncAuditLogs (
            EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion
        )
        VALUES (
            'smartlock', @RoomNumber, 'REISSUE_PIN', 'WEB_API', @PerformedBy,
            CONCAT(N'[SMARTLOCK] Cấp lại mã PIN bảo mật (SHA-256) cho phòng ', @RoomNumber, N' (Lý do: ', @Reason, N')'),
            ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 6. NÂNG CẤP BẢNG AppUsers SANG MÃ BĂM MỘT CHIỀU SALTED SHA-256 (XÓA MẬT KHẨU RÕ)
UPDATE dbo.AppUsers
SET PasswordHash = '564C07116D7DED47805641FC46710CA1724F44F37868A56169213CBB65A07DA9|177D7D30D3222D79E85E878362C8AE8B3B2FF1933DF3CB12DF85A27A402970FC'
WHERE LOWER(Username) = 'admin';

UPDATE dbo.AppUsers
SET PasswordHash = '1465521F76F3642957E4B412E40319191D256A74E97934123DD3B268FEADA82E|C1FD4E790FD8EBCC9216BE70DC10EAFDC85BD520DAC1820F4B60DC88CB2C7BA5'
WHERE LOWER(Username) = 'letan';

UPDATE dbo.AppUsers
SET PasswordHash = 'DCB6A6365E422125D9A6F92A5D2B674F68F949276610324BD813BD696871A787|9E28CDD21EA2D58E931C16F5C79AAB3F2D2E6C691681A0DF2161218D017C1962'
WHERE LOWER(Username) = 'ketoan';

UPDATE dbo.AppUsers
SET PasswordHash = '3E032EFBC4A078338B88E7E90F2F97284CB57914C26BDAC671035F240815EAB1|AB41C0B964F70E32DA2FCE2748A2B4DEEDDC968D5DACD45F714A27714095C7EE'
WHERE LOWER(Username) = 'nhansu';

UPDATE dbo.AppUsers
SET PasswordHash = '8ED86C2BC9F61C30F419F3655B6FDA16B55CF5A18B6A3A97A59B7FD2453248E5|BE562922CC11AB91A18657DCE425148427B090688284A76236B55AC4EE2C46AC'
WHERE LOWER(Username) = 'quanly';

UPDATE dbo.AppUsers
SET PasswordHash = '3FCA856599D86F9B5E2A3304A1AB027BC3EA023C06DEA7B2E509087ACFA86B70|D3312D3ADC0A4EC182E7B111D386E7B8693F6A1D7F5CF64002DB86B301434AB5'
WHERE LOWER(Username) = 'khachhang';
GO

