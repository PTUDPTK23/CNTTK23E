package com.library.api.exception;

import com.library.api.dto.ApiResponse;
import jakarta.validation.ConstraintViolationException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.ErrorResponse;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;

import java.util.LinkedHashMap;
import java.util.Map;

/** Xử lý lỗi tập trung: không bao giờ trả stack trace cho client. */
@RestControllerAdvice
@Slf4j
public class GlobalExceptionHandler {

    @ExceptionHandler(ApiException.class)
    public ResponseEntity<ApiResponse<Object>> handleApi(ApiException ex) {
        return ResponseEntity.status(ex.getStatus()).body(ApiResponse.error(ex.getMessage(), null));
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ApiResponse<Object>> handleValidation(MethodArgumentNotValidException ex) {
        Map<String, String> errors = new LinkedHashMap<>();
        ex.getBindingResult().getFieldErrors().forEach(f -> errors.putIfAbsent(f.getField(), f.getDefaultMessage()));
        return ResponseEntity.badRequest().body(ApiResponse.error("Dữ liệu không hợp lệ", errors));
    }

    @ExceptionHandler(ConstraintViolationException.class)
    public ResponseEntity<ApiResponse<Object>> handleConstraint(ConstraintViolationException ex) {
        return ResponseEntity.badRequest().body(ApiResponse.error("Dữ liệu không hợp lệ: " + ex.getMessage(), null));
    }

    @ExceptionHandler({HttpMessageNotReadableException.class, MethodArgumentTypeMismatchException.class})
    public ResponseEntity<ApiResponse<Object>> handleBadInput(Exception ex) {
        return ResponseEntity.badRequest().body(ApiResponse.error("Yêu cầu không hợp lệ (sai định dạng dữ liệu)", null));
    }

    /** Vi phạm unique/FK ở DB (lớp bảo vệ cuối cùng) => 409. */
    @ExceptionHandler(DataIntegrityViolationException.class)
    public ResponseEntity<ApiResponse<Object>> handleIntegrity(DataIntegrityViolationException ex) {
        log.warn("Vi phạm ràng buộc dữ liệu: {}", ex.getMostSpecificCause().getMessage());
        return ResponseEntity.status(HttpStatus.CONFLICT)
                .body(ApiResponse.error("Dữ liệu xung đột với bản ghi đã có (trùng hoặc đang được tham chiếu)", null));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiResponse<Object>> handleOther(Exception ex) {
        // Các lỗi chuẩn của Spring MVC (404 đường dẫn sai, 405 sai method, thiếu tham số...) giữ nguyên mã trạng thái.
        if (ex instanceof ErrorResponse er) {
            HttpStatus status = HttpStatus.resolve(er.getStatusCode().value());
            String msg = status == HttpStatus.NOT_FOUND ? "Không tìm thấy tài nguyên yêu cầu"
                    : status == HttpStatus.METHOD_NOT_ALLOWED ? "Phương thức không được hỗ trợ"
                    : "Yêu cầu không hợp lệ";
            return ResponseEntity.status(er.getStatusCode()).body(ApiResponse.error(msg, null));
        }
        log.error("Lỗi không mong đợi", ex);
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(ApiResponse.error("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", null));
    }
}
